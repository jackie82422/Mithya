using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using MockServer.Api.DTOs.Requests;
using MockServer.Core.Entities;
using MockServer.Core.Interfaces;
using MockServer.Infrastructure.WireMock;
using MockServer.Infrastructure.ProtocolHandlers;

namespace MockServer.Api.Endpoints;

public static class EndpointManagementApis
{
    public static void MapEndpointManagementApis(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/api/endpoints").WithTags("Endpoints");

        group.MapGet("/", async (IEndpointRepository repo) =>
        {
            var endpoints = await repo.GetAllAsync();
            return Results.Ok(endpoints);
        })
        .WithName("GetAllEndpoints")
        .WithOpenApi();

        group.MapGet("/{id}", async (Guid id, IEndpointRepository repo) =>
        {
            var endpoint = await repo.GetByIdAsync(id);
            return endpoint is not null ? Results.Ok(endpoint) : Results.NotFound();
        })
        .WithName("GetEndpointById")
        .WithOpenApi();

        group.MapPost("/", async (
            CreateEndpointRequest request,
            IEndpointRepository repo,
            ProtocolHandlerFactory factory,
            WireMockServerManager manager) =>
        {
            var endpoint = new MockEndpoint
            {
                Name = request.Name,
                ServiceName = request.ServiceName,
                Protocol = request.Protocol,
                Path = request.Path,
                HttpMethod = request.HttpMethod.ToUpper(),
                ProtocolSettings = request.ProtocolSettings,
                IsActive = true
            };

            var handler = factory.GetHandler(request.Protocol);
            var validation = handler.ValidateEndpoint(endpoint);

            if (!validation.IsValid)
            {
                return Results.BadRequest(new { errors = validation.Errors });
            }

            // Check for duplicate path+method combination
            var existingEndpoints = await repo.GetAllAsync();
            var duplicate = existingEndpoints.FirstOrDefault(e =>
                e.Path == endpoint.Path &&
                e.HttpMethod.Equals(endpoint.HttpMethod, StringComparison.OrdinalIgnoreCase));

            if (duplicate != null)
            {
                return Results.Conflict(new
                {
                    error = "DuplicateEndpoint",
                    message = $"Endpoint with path '{endpoint.Path}' and method '{endpoint.HttpMethod}' already exists",
                    path = endpoint.Path,
                    httpMethod = endpoint.HttpMethod,
                    existingEndpointId = duplicate.Id
                });
            }

            try
            {
                await repo.AddAsync(endpoint);
                await repo.SaveChangesAsync();
                await manager.SyncAllRulesAsync();

                return Results.Created($"/admin/api/endpoints/{endpoint.Id}", endpoint);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
            {
                // 23505 = unique_violation (fallback for race conditions in production)
                return Results.Conflict(new
                {
                    error = "DuplicateEndpoint",
                    message = $"Endpoint with path '{endpoint.Path}' and method '{endpoint.HttpMethod}' already exists",
                    path = endpoint.Path,
                    httpMethod = endpoint.HttpMethod
                });
            }
        })
        .WithName("CreateEndpoint")
        .WithOpenApi();

        group.MapPut("/{id}", async (
            Guid id,
            UpdateEndpointRequest request,
            IEndpointRepository repo,
            ProtocolHandlerFactory factory,
            WireMockServerManager manager) =>
        {
            var endpoint = await repo.GetByIdAsync(id);
            if (endpoint is null)
            {
                return Results.NotFound();
            }

            // Apply updates (Protocol is immutable)
            endpoint.Name = request.Name;
            endpoint.ServiceName = request.ServiceName;
            endpoint.Path = request.Path;
            endpoint.HttpMethod = request.HttpMethod.ToUpper();
            endpoint.ProtocolSettings = request.ProtocolSettings;

            var handler = factory.GetHandler(endpoint.Protocol);
            var validation = handler.ValidateEndpoint(endpoint);
            if (!validation.IsValid)
            {
                return Results.BadRequest(new { errors = validation.Errors });
            }

            // Check for duplicate path+method (exclude self)
            var existingEndpoints = await repo.GetAllAsync();
            var duplicate = existingEndpoints.FirstOrDefault(e =>
                e.Id != id &&
                e.Path == endpoint.Path &&
                e.HttpMethod.Equals(endpoint.HttpMethod, StringComparison.OrdinalIgnoreCase));

            if (duplicate != null)
            {
                return Results.Conflict(new
                {
                    error = "DuplicateEndpoint",
                    message = $"Endpoint with path '{endpoint.Path}' and method '{endpoint.HttpMethod}' already exists",
                    path = endpoint.Path,
                    httpMethod = endpoint.HttpMethod,
                    existingEndpointId = duplicate.Id
                });
            }

            await repo.UpdateAsync(endpoint);
            await repo.SaveChangesAsync();
            await manager.SyncAllRulesAsync();

            return Results.Ok(endpoint);
        })
        .WithName("UpdateEndpoint")
        .WithOpenApi();

        group.MapPut("/{id}/default", async (
            Guid id,
            SetDefaultResponseRequest request,
            IEndpointRepository repo,
            WireMockServerManager manager) =>
        {
            var endpoint = await repo.GetByIdAsync(id);
            if (endpoint is null)
            {
                return Results.NotFound();
            }

            endpoint.DefaultResponse = request.ResponseBody;
            endpoint.DefaultStatusCode = request.StatusCode;

            await repo.UpdateAsync(endpoint);
            await repo.SaveChangesAsync();
            await manager.SyncAllRulesAsync();

            return Results.Ok(endpoint);
        })
        .WithName("SetDefaultResponse")
        .WithOpenApi();

        group.MapDelete("/{id}", async (
            Guid id,
            IEndpointRepository repo,
            WireMockServerManager manager) =>
        {
            var endpoint = await repo.GetByIdAsync(id);
            if (endpoint is null)
            {
                return Results.NotFound();
            }

            await repo.DeleteAsync(id);
            await repo.SaveChangesAsync();
            await manager.SyncAllRulesAsync();

            return Results.NoContent();
        })
        .WithName("DeleteEndpoint")
        .WithOpenApi();

        group.MapMethods("/{id}/toggle", new[] { "PATCH" }, async (
            Guid id,
            IEndpointRepository repo,
            WireMockServerManager manager) =>
        {
            var endpoint = await repo.GetByIdAsync(id);
            if (endpoint is null)
            {
                return Results.NotFound();
            }

            endpoint.IsActive = !endpoint.IsActive;
            await repo.UpdateAsync(endpoint);
            await repo.SaveChangesAsync();

            try
            {
                await manager.SyncAllRulesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: WireMock sync failed after toggling endpoint '{endpoint.Name}': {ex.Message}");
            }

            return Results.Ok(endpoint);
        })
        .WithName("ToggleEndpoint")
        .WithOpenApi();
    }
}
