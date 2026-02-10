using Microsoft.EntityFrameworkCore;
using Mithya.Core.Entities;
using Mithya.Core.Interfaces;
using Mithya.Infrastructure.Data;
using Mithya.Infrastructure.MockEngine;

namespace Mithya.Api.Endpoints;

public static class ServiceProxyApis
{
    private static List<string> ValidateServiceProxy(ServiceProxy proxy)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(proxy.ServiceName))
            errors.Add("ServiceName is required");

        if (string.IsNullOrWhiteSpace(proxy.TargetBaseUrl))
            errors.Add("TargetBaseUrl is required");
        else if (!Uri.TryCreate(proxy.TargetBaseUrl, UriKind.Absolute, out var uri)
                 || (uri.Scheme != "http" && uri.Scheme != "https"))
            errors.Add("TargetBaseUrl must be a valid HTTP/HTTPS URL");

        if (proxy.TimeoutMs < 0)
            errors.Add("TimeoutMs must be 0 or greater");

        return errors;
    }

    public static void MapServiceProxyApis(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/api/service-proxies").WithTags("ServiceProxies");

        // GET / - List all service proxies
        group.MapGet("/", async (IServiceProxyRepository repo) =>
        {
            var proxies = await repo.GetAllAsync();
            return Results.Ok(proxies);
        })
        .WithName("GetAllServiceProxies")
        .WithOpenApi();

        // GET /{id} - Get by ID
        group.MapGet("/{id}", async (Guid id, IServiceProxyRepository repo) =>
        {
            var proxy = await repo.GetByIdAsync(id);
            return proxy is not null
                ? Results.Ok(proxy)
                : Results.NotFound(new { error = "Service proxy not found" });
        })
        .WithName("GetServiceProxyById")
        .WithOpenApi();

        // GET /by-service/{serviceName} - Get by service name
        group.MapGet("/by-service/{serviceName}", async (string serviceName, IServiceProxyRepository repo) =>
        {
            var proxy = await repo.GetByServiceNameAsync(serviceName);
            return proxy is not null
                ? Results.Ok(proxy)
                : Results.NotFound(new { error = "Service proxy not found for this service" });
        })
        .WithName("GetServiceProxyByServiceName")
        .WithOpenApi();

        // POST / - Create service proxy
        group.MapPost("/", async (
            ServiceProxy request,
            IServiceProxyRepository repo,
            IServiceProxyCache cache,
            MithyaDbContext db) =>
        {
            var validationErrors = ValidateServiceProxy(request);
            if (validationErrors.Count > 0)
                return Results.BadRequest(new { errors = validationErrors });

            // Check service name exists in endpoints
            var serviceExists = await db.MockEndpoints
                .AnyAsync(e => e.ServiceName == request.ServiceName);
            if (!serviceExists)
                return Results.BadRequest(new { errors = new[] { $"No endpoints found with ServiceName '{request.ServiceName}'" } });

            // Check unique constraint
            var existing = await repo.GetByServiceNameAsync(request.ServiceName);
            if (existing != null)
                return Results.BadRequest(new { errors = new[] { $"A service proxy already exists for ServiceName '{request.ServiceName}'" } });

            await repo.AddAsync(request);
            await repo.SaveChangesAsync();
            await cache.ReloadAsync();

            return Results.Created($"/admin/api/service-proxies/{request.Id}", request);
        })
        .WithName("CreateServiceProxy")
        .WithOpenApi();

        // PUT /{id} - Update service proxy
        group.MapPut("/{id}", async (
            Guid id,
            ServiceProxy request,
            IServiceProxyRepository repo,
            IServiceProxyCache cache) =>
        {
            var validationErrors = ValidateServiceProxy(request);
            if (validationErrors.Count > 0)
                return Results.BadRequest(new { errors = validationErrors });

            var existing = await repo.GetByIdAsync(id);
            if (existing is null)
                return Results.NotFound(new { error = "Service proxy not found" });

            existing.ServiceName = request.ServiceName;
            existing.TargetBaseUrl = request.TargetBaseUrl;
            existing.IsActive = request.IsActive;
            existing.IsRecording = request.IsRecording;
            existing.ForwardHeaders = request.ForwardHeaders;
            existing.AdditionalHeaders = request.AdditionalHeaders;
            existing.TimeoutMs = request.TimeoutMs;
            existing.StripPathPrefix = request.StripPathPrefix;
            existing.FallbackEnabled = request.FallbackEnabled;

            await repo.UpdateAsync(existing);
            await repo.SaveChangesAsync();
            await cache.ReloadAsync();

            return Results.Ok(existing);
        })
        .WithName("UpdateServiceProxy")
        .WithOpenApi();

        // DELETE /{id} - Delete service proxy
        group.MapDelete("/{id}", async (
            Guid id,
            IServiceProxyRepository repo,
            IServiceProxyCache cache) =>
        {
            var proxy = await repo.GetByIdAsync(id);
            if (proxy is null)
                return Results.NotFound(new { error = "Service proxy not found" });

            await repo.DeleteAsync(id);
            await repo.SaveChangesAsync();
            await cache.ReloadAsync();

            return Results.NoContent();
        })
        .WithName("DeleteServiceProxy")
        .WithOpenApi();

        // PATCH /{id}/toggle - Toggle active
        group.MapMethods("/{id}/toggle", new[] { "PATCH" }, async (
            Guid id,
            IServiceProxyRepository repo,
            IServiceProxyCache cache) =>
        {
            var proxy = await repo.GetByIdAsync(id);
            if (proxy is null)
                return Results.NotFound(new { error = "Service proxy not found" });

            proxy.IsActive = !proxy.IsActive;
            await repo.UpdateAsync(proxy);
            await repo.SaveChangesAsync();
            await cache.ReloadAsync();

            return Results.Ok(proxy);
        })
        .WithName("ToggleServiceProxy")
        .WithOpenApi();

        // PATCH /{id}/toggle-recording - Toggle recording
        group.MapMethods("/{id}/toggle-recording", new[] { "PATCH" }, async (
            Guid id,
            IServiceProxyRepository repo,
            IServiceProxyCache cache) =>
        {
            var proxy = await repo.GetByIdAsync(id);
            if (proxy is null)
                return Results.NotFound(new { error = "Service proxy not found" });

            proxy.IsRecording = !proxy.IsRecording;
            await repo.UpdateAsync(proxy);
            await repo.SaveChangesAsync();
            await cache.ReloadAsync();

            return Results.Ok(proxy);
        })
        .WithName("ToggleServiceProxyRecording")
        .WithOpenApi();

        // PATCH /{id}/toggle-fallback - Toggle fallback
        group.MapMethods("/{id}/toggle-fallback", new[] { "PATCH" }, async (
            Guid id,
            IServiceProxyRepository repo,
            IServiceProxyCache cache) =>
        {
            var proxy = await repo.GetByIdAsync(id);
            if (proxy is null)
                return Results.NotFound(new { error = "Service proxy not found" });

            proxy.FallbackEnabled = !proxy.FallbackEnabled;
            await repo.UpdateAsync(proxy);
            await repo.SaveChangesAsync();
            await cache.ReloadAsync();

            return Results.Ok(proxy);
        })
        .WithName("ToggleServiceProxyFallback")
        .WithOpenApi();

        // GET /services - List distinct service names from endpoints
        group.MapGet("/services", async (MithyaDbContext db, IServiceProxyRepository repo) =>
        {
            var proxies = await repo.GetAllAsync();
            var proxyServiceNames = proxies
                .Select(p => p.ServiceName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var services = await db.MockEndpoints
                .GroupBy(e => e.ServiceName)
                .Select(g => new
                {
                    ServiceName = g.Key,
                    EndpointCount = g.Count()
                })
                .ToListAsync();

            var result = services.Select(s => new
            {
                s.ServiceName,
                s.EndpointCount,
                HasProxy = proxyServiceNames.Contains(s.ServiceName)
            });

            return Results.Ok(result);
        })
        .WithName("GetAvailableServices")
        .WithOpenApi();
    }
}
