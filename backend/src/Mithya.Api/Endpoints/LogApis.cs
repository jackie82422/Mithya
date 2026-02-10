using Mithya.Core.Interfaces;

namespace Mithya.Api.Endpoints;

public static class LogApis
{
    public static void MapLogApis(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/api/logs").WithTags("Logs");

        group.MapGet("/", async (
            IRequestLogRepository repo,
            int limit = 100) =>
        {
            var logs = await repo.GetLogsAsync(limit);
            return Results.Ok(logs);
        })
        .WithName("GetLogs")
        .WithOpenApi();

        group.MapDelete("/", async (IRequestLogRepository repo) =>
        {
            await repo.DeleteAllAsync();
            return Results.NoContent();
        })
        .WithName("DeleteAllLogs")
        .WithOpenApi();

        group.MapGet("/endpoint/{endpointId}", async (
            Guid endpointId,
            IRequestLogRepository repo,
            int limit = 100) =>
        {
            var logs = await repo.GetLogsByEndpointAsync(endpointId, limit);
            return Results.Ok(logs);
        })
        .WithName("GetLogsByEndpoint")
        .WithOpenApi();
    }
}
