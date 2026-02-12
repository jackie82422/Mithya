using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Mithya.Api.DTOs.Requests;
using Mithya.Core.Entities;
using Mithya.Core.Enums;
using Mithya.Core.Interfaces;
using Mithya.Infrastructure.ProtocolHandlers;

namespace Mithya.Api.Endpoints;

public static class EndpointManagementApis
{
    private static readonly string[] ValidHttpMethods = { "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS" };
    private static readonly Regex HtmlTagPattern = new(@"<[^>]+>", RegexOptions.Compiled);

    private static List<string> ValidateEndpointRequest(string name, string path, string httpMethod, int? protocol = null, string? serviceName = null)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(name))
            errors.Add("Name is required");
        else if (name.Length > 200)
            errors.Add("Name must be 200 characters or less");
        else if (HtmlTagPattern.IsMatch(name))
            errors.Add("Name must not contain HTML tags");

        if (string.IsNullOrWhiteSpace(path))
            errors.Add("Path is required");
        else if (path.Length > 500)
            errors.Add("Path must be 500 characters or less");

        if (serviceName != null && serviceName.Length > 200)
            errors.Add("ServiceName must be 200 characters or less");
        if (serviceName != null && HtmlTagPattern.IsMatch(serviceName))
            errors.Add("ServiceName must not contain HTML tags");

        if (string.IsNullOrWhiteSpace(httpMethod))
            errors.Add("HttpMethod is required");
        else if (!ValidHttpMethods.Contains(httpMethod.ToUpper()))
            errors.Add($"HttpMethod must be one of: {string.Join(", ", ValidHttpMethods)}");

        if (protocol.HasValue && !Enum.IsDefined(typeof(ProtocolType), protocol.Value))
            errors.Add($"Protocol must be one of: {string.Join(", ", Enum.GetValues<ProtocolType>().Select(p => $"{(int)p}({p})"))}");

        return errors;
    }

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
            return endpoint is not null
                ? Results.Ok(endpoint)
                : Results.NotFound(new { error = "Endpoint not found" });
        })
        .WithName("GetEndpointById")
        .WithOpenApi();

        group.MapPost("/", async (
            CreateEndpointRequest request,
            IEndpointRepository repo,
            ProtocolHandlerFactory factory,
            IMockRuleCache cache) =>
        {
            // Input validation
            var validationErrors = ValidateEndpointRequest(request.Name, request.Path, request.HttpMethod, (int)request.Protocol, request.ServiceName);
            if (validationErrors.Count > 0)
                return Results.BadRequest(new { errors = validationErrors });

            // Auto-normalize path
            if (!request.Path.StartsWith("/"))
                request.Path = "/" + request.Path;

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
                await cache.ReloadEndpointAsync(endpoint.Id);

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
            IMockRuleCache cache) =>
        {
            var endpoint = await repo.GetByIdAsync(id);
            if (endpoint is null)
            {
                return Results.NotFound(new { error = "Endpoint not found" });
            }

            // Input validation
            var validationErrors = ValidateEndpointRequest(request.Name, request.Path, request.HttpMethod, serviceName: request.ServiceName);
            if (validationErrors.Count > 0)
                return Results.BadRequest(new { errors = validationErrors });

            if (!request.Path.StartsWith("/"))
                request.Path = "/" + request.Path;

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
            await cache.ReloadEndpointAsync(endpoint.Id);

            return Results.Ok(endpoint);
        })
        .WithName("UpdateEndpoint")
        .WithOpenApi();

        group.MapPut("/{id}/default", async (
            Guid id,
            SetDefaultResponseRequest request,
            IEndpointRepository repo,
            IMockRuleCache cache) =>
        {
            var endpoint = await repo.GetByIdAsync(id);
            if (endpoint is null)
            {
                return Results.NotFound(new { error = "Endpoint not found" });
            }

            endpoint.DefaultResponse = request.ResponseBody;
            endpoint.DefaultStatusCode = request.StatusCode;

            await repo.UpdateAsync(endpoint);
            await repo.SaveChangesAsync();
            await cache.ReloadEndpointAsync(endpoint.Id);

            return Results.Ok(endpoint);
        })
        .WithName("SetDefaultResponse")
        .WithOpenApi();

        group.MapDelete("/{id}", async (
            Guid id,
            IEndpointRepository repo,
            IMockRuleCache cache) =>
        {
            var endpoint = await repo.GetByIdAsync(id);
            if (endpoint is null)
            {
                return Results.NotFound(new { error = "Endpoint not found" });
            }

            await repo.DeleteAsync(id);
            await repo.SaveChangesAsync();
            cache.RemoveEndpoint(id);

            return Results.NoContent();
        })
        .WithName("DeleteEndpoint")
        .WithOpenApi();

        group.MapMethods("/{id}/toggle", new[] { "PATCH" }, async (
            Guid id,
            IEndpointRepository repo,
            IMockRuleCache cache) =>
        {
            var endpoint = await repo.GetByIdAsync(id);
            if (endpoint is null)
            {
                return Results.NotFound(new { error = "Endpoint not found" });
            }

            endpoint.IsActive = !endpoint.IsActive;
            await repo.UpdateAsync(endpoint);
            await repo.SaveChangesAsync();
            await cache.ReloadEndpointAsync(endpoint.Id);

            return Results.Ok(endpoint);
        })
        .WithName("ToggleEndpoint")
        .WithOpenApi();
    }
}
