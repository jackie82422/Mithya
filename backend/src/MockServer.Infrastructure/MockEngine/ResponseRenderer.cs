using Microsoft.AspNetCore.Http;
using MockServer.Core.Enums;
using MockServer.Core.Interfaces;

namespace MockServer.Infrastructure.MockEngine;

public class ResponseRenderer
{
    public async Task RenderAsync(HttpContext httpContext, MatchResult matchResult)
    {
        var response = httpContext.Response;

        if (matchResult.IsDefaultResponse)
        {
            response.StatusCode = matchResult.Endpoint.DefaultStatusCode ?? 200;

            // Set default content type based on protocol
            SetDefaultContentType(response, matchResult.Endpoint.Protocol);

            await response.WriteAsync(matchResult.Endpoint.DefaultResponse ?? string.Empty);
            return;
        }

        var rule = matchResult.Rule!;

        // Apply delay if configured
        if (rule.DelayMs > 0)
        {
            await Task.Delay(rule.DelayMs);
        }

        response.StatusCode = rule.ResponseStatusCode;

        // Apply custom headers
        if (rule.ResponseHeaders != null)
        {
            foreach (var header in rule.ResponseHeaders)
            {
                response.Headers[header.Key] = header.Value;
            }
        }

        // Set default content type if not already set by custom headers
        if (!response.Headers.ContainsKey("Content-Type"))
        {
            SetDefaultContentType(response, matchResult.Endpoint.Protocol);
        }

        await response.WriteAsync(rule.ResponseBody ?? string.Empty);
    }

    private static void SetDefaultContentType(HttpResponse response, ProtocolType protocol)
    {
        response.ContentType = protocol == ProtocolType.SOAP
            ? "text/xml; charset=utf-8"
            : "application/json";
    }
}
