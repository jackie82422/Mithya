using Newtonsoft.Json;
using MockServer.Core.Interfaces;
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
    private readonly IEndpointRepository _endpointRepo;
    private readonly IRuleRepository _ruleRepo;
    private readonly ProtocolHandlerFactory _factory;
    private WireMockServer? _server;
    private readonly int _port;

    public WireMockServerManager(
        IEndpointRepository endpointRepo,
        IRuleRepository ruleRepo,
        ProtocolHandlerFactory factory,
        int port = 5001)
    {
        _endpointRepo = endpointRepo;
        _ruleRepo = ruleRepo;
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
            StartAdminInterface = false,
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

        var endpoints = await _endpointRepo.GetAllActiveAsync();

        foreach (var endpoint in endpoints)
        {
            var rules = await _ruleRepo.GetByEndpointIdAsync(endpoint.Id);

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

                    Console.WriteLine($"Synced rule '{rule.RuleName}' for endpoint '{endpoint.Path}' with priority {rule.Priority}");
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
        }
        builder.WithPath(pathPattern);

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
                            builder.WithBody(new WMMatchers.JsonPathMatcher(condition.FieldPath, condition.Value));
                        }
                        else if (endpoint.Protocol == CoreEnums.ProtocolType.SOAP)
                        {
                            builder.WithBody(new WMMatchers.XPathMatcher(condition.FieldPath, condition.Value));
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
