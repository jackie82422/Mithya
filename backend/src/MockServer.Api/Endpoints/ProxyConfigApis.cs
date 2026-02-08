using MockServer.Core.Entities;
using MockServer.Core.Interfaces;
using MockServer.Infrastructure.MockEngine;

namespace MockServer.Api.Endpoints;

public static class ProxyConfigApis
{
    public static void MapProxyConfigApis(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/api/proxy-configs").WithTags("ProxyConfigs");

        group.MapGet("/", async (IProxyConfigRepository repo) =>
        {
            var configs = await repo.GetAllAsync();
            return Results.Ok(configs);
        })
        .WithName("GetAllProxyConfigs")
        .WithOpenApi();

        group.MapGet("/{id}", async (Guid id, IProxyConfigRepository repo) =>
        {
            var config = await repo.GetByIdAsync(id);
            return config is not null ? Results.Ok(config) : Results.NotFound();
        })
        .WithName("GetProxyConfigById")
        .WithOpenApi();

        group.MapPost("/", async (
            ProxyConfig request,
            IProxyConfigRepository repo,
            IProxyConfigCache cache) =>
        {
            if (string.IsNullOrWhiteSpace(request.TargetBaseUrl))
                return Results.BadRequest(new { error = "TargetBaseUrl is required" });

            await repo.AddAsync(request);
            await repo.SaveChangesAsync();
            await cache.ReloadAsync();

            return Results.Created($"/admin/api/proxy-configs/{request.Id}", request);
        })
        .WithName("CreateProxyConfig")
        .WithOpenApi();

        group.MapPut("/{id}", async (
            Guid id,
            ProxyConfig request,
            IProxyConfigRepository repo,
            IProxyConfigCache cache) =>
        {
            var existing = await repo.GetByIdAsync(id);
            if (existing is null)
                return Results.NotFound();

            existing.TargetBaseUrl = request.TargetBaseUrl;
            existing.EndpointId = request.EndpointId;
            existing.IsActive = request.IsActive;
            existing.IsRecording = request.IsRecording;
            existing.ForwardHeaders = request.ForwardHeaders;
            existing.AdditionalHeaders = request.AdditionalHeaders;
            existing.TimeoutMs = request.TimeoutMs;
            existing.StripPathPrefix = request.StripPathPrefix;

            await repo.UpdateAsync(existing);
            await repo.SaveChangesAsync();
            await cache.ReloadAsync();

            return Results.Ok(existing);
        })
        .WithName("UpdateProxyConfig")
        .WithOpenApi();

        group.MapDelete("/{id}", async (
            Guid id,
            IProxyConfigRepository repo,
            IProxyConfigCache cache) =>
        {
            var config = await repo.GetByIdAsync(id);
            if (config is null)
                return Results.NotFound();

            await repo.DeleteAsync(id);
            await repo.SaveChangesAsync();
            await cache.ReloadAsync();

            return Results.NoContent();
        })
        .WithName("DeleteProxyConfig")
        .WithOpenApi();

        group.MapMethods("/{id}/toggle", new[] { "PATCH" }, async (
            Guid id,
            IProxyConfigRepository repo,
            IProxyConfigCache cache) =>
        {
            var config = await repo.GetByIdAsync(id);
            if (config is null)
                return Results.NotFound();

            config.IsActive = !config.IsActive;
            await repo.UpdateAsync(config);
            await repo.SaveChangesAsync();
            await cache.ReloadAsync();

            return Results.Ok(config);
        })
        .WithName("ToggleProxyConfig")
        .WithOpenApi();

        group.MapMethods("/{id}/toggle-recording", new[] { "PATCH" }, async (
            Guid id,
            IProxyConfigRepository repo,
            IProxyConfigCache cache) =>
        {
            var config = await repo.GetByIdAsync(id);
            if (config is null)
                return Results.NotFound();

            config.IsRecording = !config.IsRecording;
            await repo.UpdateAsync(config);
            await repo.SaveChangesAsync();
            await cache.ReloadAsync();

            return Results.Ok(config);
        })
        .WithName("ToggleProxyConfigRecording")
        .WithOpenApi();
    }
}
