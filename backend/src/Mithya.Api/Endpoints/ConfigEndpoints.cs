namespace Mithya.Api.Endpoints;

public static class ConfigEndpoints
{
    public static void MapConfigEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/api/config").WithTags("Config");

        group.MapGet("/", (IConfiguration config, HttpContext context) =>
        {
            // Allow explicit override via config/env (e.g. Mithya__BaseUrl=http://example.com:5050)
            var baseUrlOverride = config.GetValue<string>("Mithya:BaseUrl");

            string mithyaUrl;
            string mithyaHost;
            int mithyaPort;

            if (!string.IsNullOrEmpty(baseUrlOverride))
            {
                var uri = new Uri(baseUrlOverride.TrimEnd('/'));
                mithyaUrl = uri.GetLeftPart(UriPartial.Authority);
                mithyaHost = uri.Host;
                mithyaPort = uri.Port;
            }
            else
            {
                var scheme = context.Request.Scheme;
                mithyaUrl = $"{scheme}://{context.Request.Host}";
                mithyaHost = context.Request.Host.Host;
                mithyaPort = context.Request.Host.Port ?? (scheme == "https" ? 443 : 80);
            }

            return Results.Ok(new
            {
                MithyaPort = mithyaPort,
                MithyaUrl = mithyaUrl,
                MithyaHost = mithyaHost,
                AdminApiUrl = mithyaUrl
            });
        })
        .WithName("GetServerConfig")
        .WithOpenApi()
        .WithDescription("Get Mithya configuration including URL and port information");
    }
}
