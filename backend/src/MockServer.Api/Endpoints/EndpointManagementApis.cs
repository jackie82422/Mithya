using Newtonsoft.Json;
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

            await repo.AddAsync(endpoint);
            await repo.SaveChangesAsync();
            await manager.SyncAllRulesAsync();

            return Results.Created($"/admin/api/endpoints/{endpoint.Id}", endpoint);
        })
        .WithName("CreateEndpoint")
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
    }
}
