using Newtonsoft.Json;
using MockServer.Api.DTOs.Requests;
using MockServer.Core.Entities;
using MockServer.Core.Interfaces;
using MockServer.Infrastructure.WireMock;
using MockServer.Infrastructure.ProtocolHandlers;

namespace MockServer.Api.Endpoints;

public static class RuleManagementApis
{
    public static void MapRuleManagementApis(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/api/endpoints/{endpointId}/rules").WithTags("Rules");

        group.MapGet("/", async (Guid endpointId, IRuleRepository repo) =>
        {
            var rules = await repo.GetByEndpointIdAsync(endpointId);
            return Results.Ok(rules);
        })
        .WithName("GetRulesByEndpoint")
        .WithOpenApi();

        group.MapPost("/", async (
            Guid endpointId,
            CreateRuleRequest request,
            IEndpointRepository endpointRepo,
            IRuleRepository ruleRepo,
            ProtocolHandlerFactory factory,
            WireMockServerManager manager) =>
        {
            var endpoint = await endpointRepo.GetByIdAsync(endpointId);
            if (endpoint is null)
            {
                return Results.NotFound(new { message = "Endpoint not found" });
            }

            var rule = new MockRule
            {
                EndpointId = endpointId,
                RuleName = request.RuleName,
                Priority = request.Priority,
                MatchConditions = JsonConvert.SerializeObject(request.Conditions),
                ResponseStatusCode = request.StatusCode,
                ResponseBody = request.ResponseBody,
                ResponseHeaders = request.ResponseHeaders != null
                    ? JsonConvert.SerializeObject(request.ResponseHeaders)
                    : null,
                DelayMs = request.DelayMs,
                IsActive = true
            };

            var handler = factory.GetHandler(endpoint.Protocol);
            var validation = handler.ValidateRule(rule, endpoint);

            if (!validation.IsValid)
            {
                return Results.BadRequest(new { errors = validation.Errors });
            }

            await ruleRepo.AddAsync(rule);
            await ruleRepo.SaveChangesAsync();
            await manager.SyncAllRulesAsync();

            return Results.Created($"/admin/api/rules/{rule.Id}", rule);
        })
        .WithName("CreateRule")
        .WithOpenApi();

        group.MapDelete("/{ruleId}", async (
            Guid endpointId,
            Guid ruleId,
            IRuleRepository repo,
            WireMockServerManager manager) =>
        {
            var rule = await repo.GetByIdAsync(ruleId);
            if (rule is null || rule.EndpointId != endpointId)
            {
                return Results.NotFound();
            }

            await repo.DeleteAsync(ruleId);
            await repo.SaveChangesAsync();
            await manager.SyncAllRulesAsync();

            return Results.NoContent();
        })
        .WithName("DeleteRule")
        .WithOpenApi();
    }
}
