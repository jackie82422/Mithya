using Mithya.Core.Entities;
using Mithya.Core.Interfaces;

namespace Mithya.Api.Endpoints;

public static class EndpointGroupApis
{
    public static void MapEndpointGroupApis(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/api/groups").WithTags("Endpoint Groups");

        // GET /admin/api/groups — 取得所有群組（含 endpoint 數量）
        group.MapGet("/", async (IEndpointGroupRepository repo) =>
        {
            var groups = await repo.GetAllAsync();
            var result = groups.Select(g => new
            {
                g.Id,
                g.Name,
                g.Description,
                g.Color,
                EndpointCount = g.Mappings.Count,
                g.CreatedAt,
                g.UpdatedAt
            });
            return Results.Ok(result);
        })
        .WithName("GetAllGroups")
        .WithOpenApi();

        // GET /admin/api/groups/{id} — 取得單一群組（含 endpoint 列表）
        group.MapGet("/{id}", async (Guid id, IEndpointGroupRepository repo) =>
        {
            var g = await repo.GetByIdWithEndpointsAsync(id);
            if (g is null)
                return Results.NotFound(new { error = "Group not found" });

            return Results.Ok(new
            {
                g.Id,
                g.Name,
                g.Description,
                g.Color,
                Endpoints = g.Mappings.Select(m => m.Endpoint),
                g.CreatedAt,
                g.UpdatedAt
            });
        })
        .WithName("GetGroupById")
        .WithOpenApi();

        // POST /admin/api/groups — 建立群組
        group.MapPost("/", async (EndpointGroup request, IEndpointGroupRepository repo) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return Results.BadRequest(new { error = "Name is required" });

            request.Mappings = new List<EndpointGroupMapping>();

            await repo.AddAsync(request);
            await repo.SaveChangesAsync();

            return Results.Created($"/admin/api/groups/{request.Id}", new
            {
                request.Id,
                request.Name,
                request.Description,
                request.Color,
                EndpointCount = 0,
                request.CreatedAt,
                request.UpdatedAt
            });
        })
        .WithName("CreateGroup")
        .WithOpenApi();

        // PUT /admin/api/groups/{id} — 更新群組
        group.MapPut("/{id}", async (Guid id, EndpointGroup request, IEndpointGroupRepository repo) =>
        {
            var existing = await repo.GetByIdAsync(id);
            if (existing is null)
                return Results.NotFound(new { error = "Group not found" });

            existing.Name = request.Name;
            existing.Description = request.Description;
            existing.Color = request.Color;

            await repo.UpdateAsync(existing);
            await repo.SaveChangesAsync();

            return Results.Ok(existing);
        })
        .WithName("UpdateGroup")
        .WithOpenApi();

        // DELETE /admin/api/groups/{id} — 刪除群組（不刪除 endpoint）
        group.MapDelete("/{id}", async (Guid id, IEndpointGroupRepository repo) =>
        {
            var existing = await repo.GetByIdAsync(id);
            if (existing is null)
                return Results.NotFound(new { error = "Group not found" });

            await repo.DeleteAsync(id);
            await repo.SaveChangesAsync();

            return Results.NoContent();
        })
        .WithName("DeleteGroup")
        .WithOpenApi();

        // POST /admin/api/groups/{id}/endpoints — 加入 endpoint 到群組
        group.MapPost("/{id}/endpoints", async (
            Guid id,
            AddEndpointsRequest request,
            IEndpointGroupRepository repo) =>
        {
            var existing = await repo.GetByIdAsync(id);
            if (existing is null)
                return Results.NotFound(new { error = "Group not found" });

            if (request.EndpointIds == null || request.EndpointIds.Count == 0)
                return Results.BadRequest(new { error = "EndpointIds is required" });

            await repo.AddEndpointsAsync(id, request.EndpointIds);
            await repo.SaveChangesAsync();

            var updated = await repo.GetByIdWithEndpointsAsync(id);
            return Results.Ok(new
            {
                updated!.Id,
                updated.Name,
                updated.Description,
                updated.Color,
                Endpoints = updated.Mappings.Select(m => m.Endpoint),
                updated.CreatedAt,
                updated.UpdatedAt
            });
        })
        .WithName("AddEndpointsToGroup")
        .WithOpenApi();

        // DELETE /admin/api/groups/{id}/endpoints/{endpointId} — 從群組移除 endpoint
        group.MapDelete("/{id}/endpoints/{endpointId}", async (
            Guid id,
            Guid endpointId,
            IEndpointGroupRepository repo) =>
        {
            var existing = await repo.GetByIdAsync(id);
            if (existing is null)
                return Results.NotFound(new { error = "Group not found" });

            await repo.RemoveEndpointAsync(id, endpointId);
            await repo.SaveChangesAsync();

            return Results.NoContent();
        })
        .WithName("RemoveEndpointFromGroup")
        .WithOpenApi();

        // GET /admin/api/endpoints/{id}/groups — 取得某 endpoint 所屬的群組
        app.MapGet("/admin/api/endpoints/{id}/groups", async (
            Guid id,
            IEndpointGroupRepository repo) =>
        {
            var groups = await repo.GetGroupsByEndpointIdAsync(id);
            return Results.Ok(groups.Select(g => new
            {
                g.Id,
                g.Name,
                g.Description,
                g.Color,
                g.CreatedAt,
                g.UpdatedAt
            }));
        })
        .WithName("GetGroupsByEndpoint")
        .WithTags("Endpoint Groups")
        .WithOpenApi();
    }
}

public record AddEndpointsRequest(List<Guid> EndpointIds);
