using MockServer.Core.Interfaces;
using MockServer.Infrastructure.WireMock;

namespace MockServer.Api.Endpoints;

public static class LogApis
{
    public static void MapLogApis(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/api/logs").WithTags("Logs");

        group.MapGet("/", async (
            IRequestLogRepository repo,
            WireMockServerManager manager,
            int limit = 100) =>
        {
            // Process new log entries from WireMock before fetching
            await manager.ProcessNewLogEntriesAsync();

            var logs = await repo.GetLogsAsync(limit);
            return Results.Ok(logs);
        })
        .WithName("GetLogs")
        .WithOpenApi();

        group.MapGet("/endpoint/{endpointId}", async (
            Guid endpointId,
            IRequestLogRepository repo,
            WireMockServerManager manager,
            int limit = 100) =>
        {
            // Process new log entries from WireMock before fetching
            await manager.ProcessNewLogEntriesAsync();

            var logs = await repo.GetLogsByEndpointAsync(endpointId, limit);
            return Results.Ok(logs);
        })
        .WithName("GetLogsByEndpoint")
        .WithOpenApi();
    }
}
