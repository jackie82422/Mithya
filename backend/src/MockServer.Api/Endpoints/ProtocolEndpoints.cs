using MockServer.Infrastructure.ProtocolHandlers;

namespace MockServer.Api.Endpoints;

public static class ProtocolEndpoints
{
    public static void MapProtocolEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/api/protocols").WithTags("Protocols");

        group.MapGet("/", (ProtocolHandlerFactory factory) =>
        {
            var schemas = factory.GetAllSchemas();
            return Results.Ok(schemas);
        })
        .WithName("GetProtocols")
        .WithOpenApi();
    }
}
