using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using MockServer.Core.Interfaces;
using MockServer.Core.Entities;
using MockServer.Infrastructure.ProtocolHandlers;
using WireMock.Server;
using WireMock.Settings;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using CoreEnums = MockServer.Core.Enums;
using WMMatchers = global::WireMock.Matchers;

namespace MockServer.Infrastructure.WireMock;

public class WireMockServerManager : IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ProtocolHandlerFactory _factory;
    private WireMockServer? _server;
    private readonly int _port;
    private readonly HashSet<Guid> _processedLogEntries = new();

    public WireMockServerManager(
        IServiceScopeFactory scopeFactory,
        ProtocolHandlerFactory factory,
        int port = 5001)
    {
        _scopeFactory = scopeFactory;
        _factory = factory;
        _port = port;
    }

    public void Start()
    {
        // Don't start server if port is 0 (test environment)
        if (_port <= 0)
        {
            return;
        }

        if (_server != null)
        {
            return;
        }

        _server = WireMockServer.Start(new WireMockServerSettings
        {
            Port = _port,
            StartAdminInterface = true,
            ReadStaticMappings = false
        });

        Console.WriteLine($"WireMock server started on port {_port}");
    }

    public void Stop()
    {
        _server?.Stop();
        _server?.Dispose();
        _server = null;
    }

    public async Task SyncAllRulesAsync()
    {
        // Don't sync if server is not started (test environment)
        if (_server == null)
        {
            return;
        }

        // Reset all existing mappings
        _server.Reset();

        // Create new scope to get fresh repositories (avoid Scoped lifetime issues in Singleton)
        using var scope = _scopeFactory.CreateScope();
        var endpointRepo = scope.ServiceProvider.GetRequiredService<IEndpointRepository>();
        var ruleRepo = scope.ServiceProvider.GetRequiredService<IRuleRepository>();

        var endpoints = await endpointRepo.GetAllActiveAsync();

        foreach (var endpoint in endpoints)
        {
            var rules = await ruleRepo.GetByEndpointIdAsync(endpoint.Id);

            // Add rules by priority (lower number = higher priority)
            foreach (var rule in rules.Where(r => r.IsActive).OrderBy(r => r.Priority))
            {
                try
                {
                    var handler = _factory.GetHandler(endpoint.Protocol);
                    var mapping = handler.ToWireMockMapping(rule, endpoint);

                    // Use WireMock fluent API to add mapping
                    var requestBuilder = CreateRequestBuilder(endpoint, rule);
                    var responseBuilder = CreateResponseBuilder(rule);

                    _server
                        .Given(requestBuilder)
                        .AtPriority(rule.Priority)
                        .RespondWith(responseBuilder);

                    // Debug: log conditions
                    var debugConditions = JsonConvert.DeserializeObject<List<Core.Entities.MatchCondition>>(rule.MatchConditions);
                    if (debugConditions != null)
                    {
                        foreach (var dc in debugConditions)
                        {
                            if (dc.SourceType == CoreEnums.FieldSourceType.Body)
                            {
                                var rp = dc.FieldPath.StartsWith("$.") ? dc.FieldPath.Substring(2) : dc.FieldPath;
                                Console.WriteLine($"  Body condition: JsonPath=$[?(@.{rp}=='{dc.Value}')]");
                            }
                        }
                    }
                    var dbgPath = endpoint.Path;
                    if (dbgPath.Contains("{"))
                        dbgPath = System.Text.RegularExpressions.Regex.Replace(dbgPath, @"\{[^}]+\}", "*");
                    Console.WriteLine($"Synced rule '{rule.RuleName}' for endpoint path='{dbgPath}' method={endpoint.HttpMethod} priority={rule.Priority}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error syncing rule '{rule.RuleName}': {ex.Message}");
                }
            }

            // Add default response (lowest priority = 999)
            if (!string.IsNullOrEmpty(endpoint.DefaultResponse))
            {
                try
                {
                    var defaultRequestBuilder = Request.Create()
                        .WithPath(endpoint.Path)
                        .UsingMethod(endpoint.HttpMethod);

                    var defaultResponseBuilder = Response.Create()
                        .WithStatusCode(endpoint.DefaultStatusCode ?? 200)
                        .WithBody(endpoint.DefaultResponse);

                    if (endpoint.Protocol == CoreEnums.ProtocolType.SOAP)
                    {
                        defaultResponseBuilder.WithHeader("Content-Type", "text/xml; charset=utf-8");
                    }
                    else
                    {
                        defaultResponseBuilder.WithHeader("Content-Type", "application/json");
                    }

                    _server
                        .Given(defaultRequestBuilder)
                        .AtPriority(999)
                        .RespondWith(defaultResponseBuilder);

                    Console.WriteLine($"Synced default response for endpoint '{endpoint.Path}' with priority 999");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error syncing default response for '{endpoint.Path}': {ex.Message}");
                }
            }
        }

        Console.WriteLine($"Sync completed. Total active endpoints: {endpoints.Count()}");
    }

    public async Task ProcessNewLogEntriesAsync()
    {
        // Don't process if server is not started (test environment)
        if (_server == null)
        {
            return;
        }

        // Create new scope to get fresh repositories
        using var scope = _scopeFactory.CreateScope();
        var endpointRepo = scope.ServiceProvider.GetRequiredService<IEndpointRepository>();
        var ruleRepo = scope.ServiceProvider.GetRequiredService<IRuleRepository>();
        var logRepo = scope.ServiceProvider.GetRequiredService<IRequestLogRepository>();

        var allEndpoints = await endpointRepo.GetAllAsync();

        // Get all log entries from WireMock
        var logEntries = _server.LogEntries.ToList();

        foreach (var entry in logEntries)
        {
            // Skip if already processed
            if (_processedLogEntries.Contains(entry.Guid))
            {
                continue;
            }

            try
            {
                var request = entry.RequestMessage;
                var response = entry.ResponseMessage;

                // Try to match the request to an endpoint
                var matchedEndpoint = allEndpoints.FirstOrDefault(e =>
                    e.IsActive &&
                    request.Method?.Equals(e.HttpMethod, StringComparison.OrdinalIgnoreCase) == true &&
                    request.Path.StartsWith(e.Path.Split('{')[0].TrimEnd('/')));

                // Try to match the request to a rule (based on response)
                Guid? matchedRuleId = null;
                bool isMatched = false;

                var responseStatusCode = response.StatusCode != null ? Convert.ToInt32(response.StatusCode) : 0;

                if (matchedEndpoint != null)
                {
                    var rules = await ruleRepo.GetByEndpointIdAsync(matchedEndpoint.Id);
                    var matchedRule = rules.FirstOrDefault(r =>
                        r.IsActive &&
                        r.ResponseStatusCode == responseStatusCode &&
                        r.ResponseBody == response.BodyData?.BodyAsString);

                    if (matchedRule != null)
                    {
                        matchedRuleId = matchedRule.Id;
                        isMatched = true;
                    }
                }

                // Calculate response time (WireMock doesn't provide response timestamp, use 0 for now)
                int responseTimeMs = 0;

                // Create log entry
                var log = new MockRequestLog
                {
                    EndpointId = matchedEndpoint?.Id,
                    RuleId = matchedRuleId,
                    Method = request.Method ?? "UNKNOWN",
                    Path = request.Path,
                    QueryString = request.Query != null ? string.Join("&", request.Query.Select(q => $"{q.Key}={string.Join(",", q.Value)}")) : null,
                    Headers = request.Headers != null ? JsonConvert.SerializeObject(request.Headers) : null,
                    Body = request.Body,
                    ResponseStatusCode = responseStatusCode,
                    ResponseBody = response.BodyData?.BodyAsString,
                    ResponseTimeMs = responseTimeMs,
                    IsMatched = isMatched
                };

                await logRepo.AddAsync(log);
                await logRepo.SaveChangesAsync();

                // Mark as processed
                _processedLogEntries.Add(entry.Guid);

                Console.WriteLine($"Logged request: {log.Method} {log.Path} -> {log.ResponseStatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing log entry {entry.Guid}: {ex.Message}");
            }
        }
    }

    private IRequestBuilder CreateRequestBuilder(Core.Entities.MockEndpoint endpoint, Core.Entities.MockRule rule)
    {
        var builder = Request.Create()
            .UsingMethod(endpoint.HttpMethod);

        // Add path matching
        var pathPattern = endpoint.Path;
        if (pathPattern.Contains("{"))
        {
            // Convert path parameters to wildcard
            pathPattern = System.Text.RegularExpressions.Regex.Replace(pathPattern, @"\{[^}]+\}", "*");
            builder.WithPath(new WMMatchers.WildcardMatcher(pathPattern));
        }
        else
        {
            builder.WithPath(pathPattern);
        }

        // Add conditions from rule
        var conditions = JsonConvert.DeserializeObject<List<Core.Entities.MatchCondition>>(rule.MatchConditions);
        if (conditions != null)
        {
            foreach (var condition in conditions)
            {
                switch (condition.SourceType)
                {
                    case CoreEnums.FieldSourceType.Body:
                        if (endpoint.Protocol == CoreEnums.ProtocolType.REST)
                        {
                            // Use JmesPathMatcher for reliable nested field matching
                            // e.g. $.user.id Equals 12345 → user.id == '12345'
                            var fieldPath = condition.FieldPath;
                            var jmesPath = fieldPath.StartsWith("$.") ? fieldPath.Substring(2) : fieldPath;
                            builder.WithBody(new WMMatchers.JmesPathMatcher($"{jmesPath} == '{condition.Value}'"));
                            Console.WriteLine($"  JmesPath: {jmesPath} == '{condition.Value}'");
                        }
                        else if (endpoint.Protocol == CoreEnums.ProtocolType.SOAP)
                        {
                            builder.WithBody(new WMMatchers.XPathMatcher(condition.FieldPath));
                        }
                        break;

                    case CoreEnums.FieldSourceType.Header:
                        builder.WithHeader(condition.FieldPath, condition.Value);
                        break;

                    case CoreEnums.FieldSourceType.Query:
                        builder.WithParam(condition.FieldPath, condition.Value);
                        break;
                }
            }
        }

        return builder;
    }

    private IResponseBuilder CreateResponseBuilder(Core.Entities.MockRule rule)
    {
        var builder = Response.Create()
            .WithStatusCode(rule.ResponseStatusCode)
            .WithBody(rule.ResponseBody);

        // Add response headers
        if (!string.IsNullOrEmpty(rule.ResponseHeaders))
        {
            var headers = JsonConvert.DeserializeObject<Dictionary<string, string>>(rule.ResponseHeaders);
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    builder.WithHeader(header.Key, header.Value);
                }
            }
        }

        // Add delay
        if (rule.DelayMs > 0)
        {
            builder.WithDelay(TimeSpan.FromMilliseconds(rule.DelayMs));
        }

        return builder;
    }

    public void Dispose()
    {
        Stop();
    }
}
