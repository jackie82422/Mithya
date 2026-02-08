using System.Text;
using MockServer.Core.Entities;
using MockServer.Core.Interfaces;
using Newtonsoft.Json;

namespace MockServer.Infrastructure.MockEngine;

public class ProxyResponse
{
    public int StatusCode { get; set; }
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
    public string TargetUrl { get; set; } = string.Empty;
}

public interface IProxyEngine
{
    Task<ProxyResponse?> ForwardAsync(MockRequestContext requestContext, ProxyConfig config);
}

public class ProxyEngine : IProxyEngine
{
    private readonly IHttpClientFactory _httpClientFactory;

    private static readonly HashSet<string> HopByHopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Connection", "Keep-Alive", "Proxy-Authenticate", "Proxy-Authorization",
        "TE", "Trailers", "Transfer-Encoding", "Upgrade", "Host"
    };

    public ProxyEngine(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ProxyResponse?> ForwardAsync(MockRequestContext context, ProxyConfig config)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("ProxyClient");
            client.Timeout = TimeSpan.FromMilliseconds(config.TimeoutMs);

            var path = context.Path;
            if (!string.IsNullOrEmpty(config.StripPathPrefix))
                path = path.Replace(config.StripPathPrefix, "", StringComparison.OrdinalIgnoreCase);

            var targetUrl = config.TargetBaseUrl.TrimEnd('/') + path;
            if (!string.IsNullOrEmpty(context.QueryString))
                targetUrl += context.QueryString;

            var request = new HttpRequestMessage(new HttpMethod(context.Method), targetUrl);

            if (config.ForwardHeaders)
            {
                foreach (var header in context.Headers)
                {
                    if (!HopByHopHeaders.Contains(header.Key))
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            if (!string.IsNullOrEmpty(config.AdditionalHeaders))
            {
                var extra = JsonConvert.DeserializeObject<Dictionary<string, string>>(config.AdditionalHeaders);
                if (extra != null)
                {
                    foreach (var kvp in extra)
                        request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
                }
            }

            if (!string.IsNullOrEmpty(context.Body) &&
                !string.Equals(context.Method, "GET", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(context.Method, "HEAD", StringComparison.OrdinalIgnoreCase))
            {
                var contentType = context.Headers.TryGetValue("Content-Type", out var ct) ? ct : "application/json";
                request.Content = new StringContent(context.Body, Encoding.UTF8, contentType);
            }

            var response = await client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();
            var responseHeaders = new Dictionary<string, string>();

            foreach (var h in response.Headers)
                responseHeaders[h.Key] = string.Join(", ", h.Value);
            foreach (var h in response.Content.Headers)
                responseHeaders[h.Key] = string.Join(", ", h.Value);

            return new ProxyResponse
            {
                StatusCode = (int)response.StatusCode,
                Body = responseBody,
                Headers = responseHeaders,
                TargetUrl = targetUrl
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Proxy forward error: {ex.Message}");
            return null;
        }
    }
}
