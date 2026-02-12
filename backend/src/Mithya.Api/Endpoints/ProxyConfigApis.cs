using Mithya.Core.Entities;
using Mithya.Core.Interfaces;
using Mithya.Infrastructure.MockEngine;

namespace Mithya.Api.Endpoints;

public static class ProxyConfigApis
{
    private static List<string> ValidateProxyConfig(ProxyConfig config)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(config.TargetBaseUrl))
            errors.Add("TargetBaseUrl is required");
        else if (!Uri.TryCreate(config.TargetBaseUrl, UriKind.Absolute, out var uri)
                 || (uri.Scheme != "http" && uri.Scheme != "https"))
            errors.Add("TargetBaseUrl must be a valid HTTP/HTTPS URL");

        if (config.TimeoutMs < 0)
            errors.Add("TimeoutMs must be 0 or greater");

        return errors;
    }

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
            return config is not null
                ? Results.Ok(config)
                : Results.NotFound(new { error = "Proxy config not found" });
        })
        .WithName("GetProxyConfigById")
        .WithOpenApi();

        group.MapPost("/", async (
            ProxyConfig request,
            IProxyConfigRepository repo,
            IProxyConfigCache cache) =>
        {
            var validationErrors = ValidateProxyConfig(request);
            if (validationErrors.Count > 0)
                return Results.BadRequest(new { errors = validationErrors });

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
            var validationErrors = ValidateProxyConfig(request);
            if (validationErrors.Count > 0)
                return Results.BadRequest(new { errors = validationErrors });

            var existing = await repo.GetByIdAsync(id);
            if (existing is null)
                return Results.NotFound(new { error = "Proxy config not found" });

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
                return Results.NotFound(new { error = "Proxy config not found" });

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
                return Results.NotFound(new { error = "Proxy config not found" });

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
                return Results.NotFound(new { error = "Proxy config not found" });

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
