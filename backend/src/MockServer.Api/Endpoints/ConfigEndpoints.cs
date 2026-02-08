namespace MockServer.Api.Endpoints;

public static class ConfigEndpoints
{
    public static void MapConfigEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/api/config").WithTags("Config");

        group.MapGet("/", (IConfiguration config, HttpContext context) =>
        {
            // Allow explicit override via config/env (e.g. MockServer__BaseUrl=http://example.com:5050)
            var baseUrlOverride = config.GetValue<string>("MockServer:BaseUrl");

            string mockServerUrl;
            string mockServerHost;
            int mockServerPort;

            if (!string.IsNullOrEmpty(baseUrlOverride))
            {
                var uri = new Uri(baseUrlOverride.TrimEnd('/'));
                mockServerUrl = uri.GetLeftPart(UriPartial.Authority);
                mockServerHost = uri.Host;
                mockServerPort = uri.Port;
            }
            else
            {
                var scheme = context.Request.Scheme;
                mockServerUrl = $"{scheme}://{context.Request.Host}";
                mockServerHost = context.Request.Host.Host;
                mockServerPort = context.Request.Host.Port ?? (scheme == "https" ? 443 : 80);
            }

            return Results.Ok(new
            {
                MockServerPort = mockServerPort,
                MockServerUrl = mockServerUrl,
                MockServerHost = mockServerHost,
                AdminApiUrl = mockServerUrl
            });
        })
        .WithName("GetServerConfig")
        .WithOpenApi()
        .WithDescription("Get Mock Server configuration including URL and port information");
    }
}
