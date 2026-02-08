using Microsoft.AspNetCore.Http;
using MockServer.Core.Enums;
using MockServer.Core.Interfaces;

namespace MockServer.Infrastructure.MockEngine;

public class ResponseRenderer
{
    private readonly ITemplateEngine _templateEngine;
    private readonly IFaultInjector _faultInjector;

    public ResponseRenderer(ITemplateEngine templateEngine, IFaultInjector faultInjector)
    {
        _templateEngine = templateEngine;
        _faultInjector = faultInjector;
    }

    public async Task RenderAsync(HttpContext httpContext, MatchResult matchResult, MockRequestContext? requestContext = null, Dictionary<string, string>? pathParams = null)
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

        // Apply fault injection first
        var faultApplied = await _faultInjector.ApplyFaultAsync(httpContext, rule);
        if (faultApplied)
            return;

        // Apply delay if configured (FixedDelay)
        if (rule.DelayMs > 0)
        {
            await Task.Delay(rule.DelayMs);
        }

        response.StatusCode = rule.ResponseStatusCode;

        // Build template context if needed
        TemplateContext? templateContext = null;
        if ((rule.IsTemplate || rule.IsResponseHeadersTemplate) && requestContext != null)
        {
            templateContext = BuildTemplateContext(requestContext, pathParams);
        }

        // Apply custom headers (with optional template rendering)
        if (rule.ResponseHeaders != null)
        {
            foreach (var header in rule.ResponseHeaders)
            {
                var headerValue = header.Value;
                if (rule.IsResponseHeadersTemplate && templateContext != null)
                {
                    try
                    {
                        headerValue = _templateEngine.Render(headerValue, templateContext);
                    }
                    catch
                    {
                        // Use raw value on template error
                    }
                }
                response.Headers[header.Key] = headerValue;
            }
        }

        // Set default content type if not already set by custom headers
        if (!response.Headers.ContainsKey("Content-Type"))
        {
            SetDefaultContentType(response, matchResult.Endpoint.Protocol);
        }

        // Render body (with optional template rendering)
        var body = rule.ResponseBody ?? string.Empty;
        if (rule.IsTemplate && templateContext != null && !string.IsNullOrEmpty(body))
        {
            try
            {
                body = _templateEngine.Render(body, templateContext);
            }
            catch
            {
                // Return raw template on error, add warning header
                response.Headers["X-Template-Error"] = "true";
            }
        }

        await response.WriteAsync(body);
    }

    private static TemplateContext BuildTemplateContext(MockRequestContext requestContext, Dictionary<string, string>? pathParams)
    {
        return new TemplateContext
        {
            Request = new TemplateRequestData
            {
                Method = requestContext.Method,
                Path = requestContext.Path,
                Body = requestContext.Body,
                Headers = requestContext.Headers,
                Query = requestContext.QueryParams,
                PathParams = pathParams ?? new Dictionary<string, string>()
            }
        };
    }

    private static void SetDefaultContentType(HttpResponse response, ProtocolType protocol)
    {
        response.ContentType = protocol == ProtocolType.SOAP
            ? "text/xml; charset=utf-8"
            : "application/json";
    }
}
