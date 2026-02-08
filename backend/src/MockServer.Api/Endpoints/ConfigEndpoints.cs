namespace MockServer.Api.Endpoints;

public static class ConfigEndpoints
{
    public static void MapConfigEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/api/config").WithTags("Config");

        group.MapGet("/", (IConfiguration config, HttpContext context) =>
        {
            var port = config.GetValue<int>("WireMock:Port", 5001);

            // Get the host from the current request
            // This handles localhost, Docker, and remote deployments
            var scheme = context.Request.Scheme;
            var host = context.Request.Host.Host;

            // Allow override via environment variable for Docker scenarios
            var mockServerHost = config.GetValue<string>("WireMock:Host") ?? host;
            var mockServerUrl = $"{scheme}://{mockServerHost}:{port}";

            return Results.Ok(new
            {
                MockServerPort = port,
                MockServerUrl = mockServerUrl,
                MockServerHost = mockServerHost,
                AdminApiUrl = $"{scheme}://{context.Request.Host}"
            });
        })
        .WithName("GetServerConfig")
        .WithOpenApi()
        .WithDescription("Get Mock Server configuration including URL and port information");
    }
}
