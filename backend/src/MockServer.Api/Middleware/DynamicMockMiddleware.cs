using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using MockServer.Core.Entities;
using MockServer.Core.Enums;
using MockServer.Core.Interfaces;
using MockServer.Infrastructure.MockEngine;

namespace MockServer.Api.Middleware;

public class DynamicMockMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMatchEngine _matchEngine;
    private readonly ResponseRenderer _responseRenderer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IProxyEngine _proxyEngine;
    private readonly IRecordingService _recordingService;
    private readonly IProxyConfigCache _proxyConfigCache;

    public DynamicMockMiddleware(
        RequestDelegate next,
        IMatchEngine matchEngine,
        ResponseRenderer responseRenderer,
        IServiceScopeFactory scopeFactory,
        IProxyEngine proxyEngine,
        IRecordingService recordingService,
        IProxyConfigCache proxyConfigCache)
    {
        _next = next;
        _matchEngine = matchEngine;
        _responseRenderer = responseRenderer;
        _scopeFactory = scopeFactory;
        _proxyEngine = proxyEngine;
        _recordingService = recordingService;
        _proxyConfigCache = proxyConfigCache;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        var path = httpContext.Request.Path.Value ?? string.Empty;

        // Pass through admin API and swagger requests
        if (path.StartsWith("/admin/api/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            await _next(httpContext);
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        // Enable buffering so we can read the request body
        httpContext.Request.EnableBuffering();

        string? requestBody = null;
        using (var reader = new StreamReader(httpContext.Request.Body, leaveOpen: true))
        {
            requestBody = await reader.ReadToEndAsync();
            httpContext.Request.Body.Position = 0;
        }

        // Build MockRequestContext
        var context = new MockRequestContext
        {
            Method = httpContext.Request.Method,
            Path = httpContext.Request.Path.Value ?? string.Empty,
            QueryString = httpContext.Request.QueryString.Value,
            QueryParams = httpContext.Request.Query
                .ToDictionary(q => q.Key, q => q.Value.ToString(), StringComparer.OrdinalIgnoreCase),
            Headers = httpContext.Request.Headers
                .ToDictionary(h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase),
            Body = requestBody
        };

        // Find matching rule
        var matchResult = await _matchEngine.FindMatchAsync(context);

        string? responseBody = null;
        int responseStatusCode;
        bool isMatched;
        Guid? endpointId = null;
        Guid? ruleId = null;
        int? faultTypeApplied = null;
        bool isProxied = false;
        string? proxyTargetUrl = null;

        if (matchResult != null)
        {
            // Render matched response
            endpointId = matchResult.Endpoint.Id;
            ruleId = matchResult.Rule?.Id;
            isMatched = true;

            await _responseRenderer.RenderAsync(httpContext, matchResult, context, matchResult.PathParams);

            responseStatusCode = httpContext.Response.StatusCode;
            responseBody = matchResult.IsDefaultResponse
                ? matchResult.Endpoint.DefaultResponse
                : matchResult.Rule?.ResponseBody;

            if (matchResult.Rule?.FaultType != FaultType.None)
                faultTypeApplied = (int?)matchResult.Rule?.FaultType;
        }
        else
        {
            // No match -> try proxy
            var proxyConfig = FindActiveProxyConfig(context);
            if (proxyConfig != null)
            {
                var proxyResponse = await _proxyEngine.ForwardAsync(context, proxyConfig);
                if (proxyResponse != null)
                {
                    httpContext.Response.StatusCode = proxyResponse.StatusCode;
                    foreach (var header in proxyResponse.Headers)
                    {
                        if (!IsResponseHopByHopHeader(header.Key))
                            httpContext.Response.Headers.TryAdd(header.Key, header.Value);
                    }
                    await httpContext.Response.WriteAsync(proxyResponse.Body);

                    responseStatusCode = proxyResponse.StatusCode;
                    responseBody = proxyResponse.Body;
                    isMatched = false;
                    isProxied = true;
                    proxyTargetUrl = proxyResponse.TargetUrl;

                    if (proxyConfig.IsRecording)
                        _ = _recordingService.RecordAsync(context, proxyResponse, null);
                }
                else
                {
                    // Proxy failed
                    responseStatusCode = 502;
                    isMatched = false;
                    httpContext.Response.StatusCode = 502;
                    httpContext.Response.ContentType = "application/json";
                    responseBody = JsonConvert.SerializeObject(new { error = "Proxy request failed", path = context.Path });
                    await httpContext.Response.WriteAsync(responseBody);
                }
            }
            else
            {
                // No proxy config either -> 404
                responseStatusCode = 404;
                isMatched = false;
                httpContext.Response.StatusCode = 404;
                httpContext.Response.ContentType = "application/json";
                responseBody = JsonConvert.SerializeObject(new { error = "No matching endpoint found", path = context.Path, method = context.Method });
                await httpContext.Response.WriteAsync(responseBody);
            }
        }

        stopwatch.Stop();

        // Write log entry directly to DB
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var logRepo = scope.ServiceProvider.GetRequiredService<IRequestLogRepository>();

            var log = new MockRequestLog
            {
                EndpointId = endpointId,
                RuleId = ruleId,
                Method = context.Method,
                Path = context.Path,
                QueryString = context.QueryString,
                Headers = JsonConvert.SerializeObject(context.Headers),
                Body = requestBody,
                ResponseStatusCode = responseStatusCode,
                ResponseBody = responseBody,
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                IsMatched = isMatched,
                FaultTypeApplied = faultTypeApplied,
                IsProxied = isProxied,
                ProxyTargetUrl = proxyTargetUrl
            };

            await logRepo.AddAsync(log);
            await logRepo.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing mock request log: {ex.Message}");
        }
    }

    private ProxyConfig? FindActiveProxyConfig(MockRequestContext context)
    {
        // Check global proxy config (endpoint-specific proxy would require knowing the endpoint ID)
        return _proxyConfigCache.GetGlobalActive();
    }

    private static bool IsResponseHopByHopHeader(string headerName)
    {
        return string.Equals(headerName, "Transfer-Encoding", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(headerName, "Connection", StringComparison.OrdinalIgnoreCase);
    }
}
