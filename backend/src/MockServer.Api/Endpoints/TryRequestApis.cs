using System.Diagnostics;

namespace MockServer.Api.Endpoints;

public static class TryRequestApis
{
    private static readonly HashSet<string> ValidMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS"
    };

    private static readonly HashSet<string> MethodsWithBody = new(StringComparer.OrdinalIgnoreCase)
    {
        "POST", "PUT", "PATCH"
    };

    private const int MaxResponseBodyBytes = 5 * 1024 * 1024; // 5MB

    private static List<string> Validate(TryRequestPayload payload)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(payload.Method))
            errors.Add("method is required");
        else if (!ValidMethods.Contains(payload.Method))
            errors.Add("method must be a valid HTTP method");

        if (string.IsNullOrWhiteSpace(payload.Url))
            errors.Add("url is required");
        else if (!Uri.TryCreate(payload.Url, UriKind.Absolute, out var uri) ||
                 (uri.Scheme != "http" && uri.Scheme != "https"))
            errors.Add("url must be an absolute HTTP(S) URL");

        return errors;
    }

    public static void MapTryRequestApis(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/api/try-request").WithTags("TryRequest");

        group.MapPost("/", async (TryRequestPayload payload, IHttpClientFactory httpClientFactory, IConfiguration config, HttpContext context) =>
        {
            var errors = Validate(payload);
            if (errors.Count > 0)
                return Results.BadRequest(new { errors });

            // Rewrite URL when targeting the mock server itself (Docker: external port != internal port)
            var targetUrl = payload.Url;
            var externalBaseUrl = config.GetValue<string>("MockServer:BaseUrl")?.TrimEnd('/');
            if (!string.IsNullOrEmpty(externalBaseUrl) &&
                targetUrl.StartsWith(externalBaseUrl, StringComparison.OrdinalIgnoreCase))
            {
                var aspnetUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? $"{context.Request.Scheme}://localhost:{context.Request.Host.Port}";
                var internalBaseUrl = aspnetUrls.Split(';').First()
                    .Replace("://+:", "://localhost:")
                    .Replace("://*:", "://localhost:")
                    .TrimEnd('/');
                targetUrl = internalBaseUrl + targetUrl[externalBaseUrl.Length..];
            }

            var client = httpClientFactory.CreateClient("TryRequest");
            var request = new HttpRequestMessage(new HttpMethod(payload.Method.ToUpperInvariant()), targetUrl);

            // Set headers
            if (payload.Headers is not null)
            {
                foreach (var (key, value) in payload.Headers)
                {
                    // Try request headers first, fall back to content headers
                    if (!request.Headers.TryAddWithoutValidation(key, value))
                        request.Content?.Headers.TryAddWithoutValidation(key, value);
                }
            }

            // Set body for methods that support it
            if (!string.IsNullOrEmpty(payload.Body) && MethodsWithBody.Contains(payload.Method))
            {
                request.Content = new StringContent(payload.Body);
                request.Content.Headers.ContentType = null; // Remove default content-type

                // Re-apply headers to content if needed
                if (payload.Headers is not null)
                {
                    foreach (var (key, value) in payload.Headers)
                    {
                        request.Content.Headers.TryAddWithoutValidation(key, value);
                    }
                }
            }

            var sw = Stopwatch.StartNew();

            try
            {
                var response = await client.SendAsync(request);
                sw.Stop();

                // Merge response headers and content headers
                var headers = new Dictionary<string, string[]>();
                foreach (var header in response.Headers)
                    headers[header.Key] = header.Value.ToArray();
                foreach (var header in response.Content.Headers)
                    headers[header.Key] = header.Value.ToArray();

                // Read body with size limit
                var bodyBytes = await response.Content.ReadAsByteArrayAsync();
                string body;
                if (bodyBytes.Length > MaxResponseBodyBytes)
                {
                    body = System.Text.Encoding.UTF8.GetString(bodyBytes, 0, MaxResponseBodyBytes) + "\n(truncated)";
                }
                else
                {
                    body = System.Text.Encoding.UTF8.GetString(bodyBytes);
                }

                return Results.Ok(new TryRequestResponse(
                    (int)response.StatusCode,
                    headers,
                    body,
                    sw.ElapsedMilliseconds
                ));
            }
            catch (TaskCanceledException)
            {
                sw.Stop();
                return Results.Json(
                    new { error = "Request timed out", elapsedMs = sw.ElapsedMilliseconds },
                    statusCode: 504
                );
            }
            catch (HttpRequestException ex)
            {
                sw.Stop();
                return Results.Json(
                    new { error = $"{ex.Message}: {payload.Url}", elapsedMs = sw.ElapsedMilliseconds },
                    statusCode: 502
                );
            }
        })
        .WithName("TryRequest")
        .WithOpenApi();
    }
}

public record TryRequestPayload(
    string Method,
    string Url,
    Dictionary<string, string>? Headers,
    string? Body
);

public record TryRequestResponse(
    int StatusCode,
    Dictionary<string, string[]> Headers,
    string Body,
    long ElapsedMs
);
