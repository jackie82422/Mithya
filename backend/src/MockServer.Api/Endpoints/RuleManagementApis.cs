using Newtonsoft.Json;
using MockServer.Api.DTOs.Requests;
using MockServer.Core.Entities;
using MockServer.Core.Interfaces;
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
            IMockRuleCache cache) =>
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
                IsTemplate = request.IsTemplate,
                IsResponseHeadersTemplate = request.IsResponseHeadersTemplate,
                FaultType = request.FaultType,
                FaultConfig = request.FaultConfig,
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
            await cache.ReloadRulesForEndpointAsync(endpointId);

            return Results.Created($"/admin/api/rules/{rule.Id}", rule);
        })
        .WithName("CreateRule")
        .WithOpenApi();

        group.MapPut("/{ruleId}", async (
            Guid endpointId,
            Guid ruleId,
            CreateRuleRequest request,
            IEndpointRepository endpointRepo,
            IRuleRepository ruleRepo,
            ProtocolHandlerFactory factory,
            IMockRuleCache cache) =>
        {
            // Validate endpoint exists
            var endpoint = await endpointRepo.GetByIdAsync(endpointId);
            if (endpoint is null)
            {
                return Results.NotFound(new { message = "Endpoint not found" });
            }

            // Validate rule exists and belongs to the endpoint
            var rule = await ruleRepo.GetByIdAsync(ruleId);
            if (rule is null || rule.EndpointId != endpointId)
            {
                return Results.NotFound(new { message = "Rule not found" });
            }

            // Update rule fields
            rule.RuleName = request.RuleName;
            rule.Priority = request.Priority;
            rule.MatchConditions = JsonConvert.SerializeObject(request.Conditions);
            rule.ResponseStatusCode = request.StatusCode;
            rule.ResponseBody = request.ResponseBody;
            rule.ResponseHeaders = request.ResponseHeaders != null
                ? JsonConvert.SerializeObject(request.ResponseHeaders)
                : null;
            rule.DelayMs = request.DelayMs;
            rule.IsTemplate = request.IsTemplate;
            rule.IsResponseHeadersTemplate = request.IsResponseHeadersTemplate;
            rule.FaultType = request.FaultType;
            rule.FaultConfig = request.FaultConfig;

            // Validate updated rule
            var handler = factory.GetHandler(endpoint.Protocol);
            var validation = handler.ValidateRule(rule, endpoint);

            if (!validation.IsValid)
            {
                return Results.BadRequest(new { errors = validation.Errors });
            }

            // Save and sync
            await ruleRepo.UpdateAsync(rule);
            await ruleRepo.SaveChangesAsync();
            await cache.ReloadRulesForEndpointAsync(endpointId);

            return Results.Ok(rule);
        })
        .WithName("UpdateRule")
        .WithOpenApi();

        group.MapDelete("/{ruleId}", async (
            Guid endpointId,
            Guid ruleId,
            IRuleRepository repo,
            IMockRuleCache cache) =>
        {
            var rule = await repo.GetByIdAsync(ruleId);
            if (rule is null || rule.EndpointId != endpointId)
            {
                return Results.NotFound();
            }

            await repo.DeleteAsync(ruleId);
            await repo.SaveChangesAsync();
            await cache.ReloadRulesForEndpointAsync(endpointId);

            return Results.NoContent();
        })
        .WithName("DeleteRule")
        .WithOpenApi();

        group.MapMethods("/{ruleId}/toggle", new[] { "PATCH" }, async (
            Guid endpointId,
            Guid ruleId,
            IEndpointRepository endpointRepo,
            IRuleRepository ruleRepo,
            IMockRuleCache cache) =>
        {
            var endpoint = await endpointRepo.GetByIdAsync(endpointId);
            if (endpoint is null)
            {
                return Results.NotFound(new { message = "Endpoint not found" });
            }

            var rule = await ruleRepo.GetByIdAsync(ruleId);
            if (rule is null)
            {
                return Results.NotFound(new { message = "Rule not found" });
            }

            if (rule.EndpointId != endpointId)
            {
                return Results.BadRequest(new { message = "Rule does not belong to this endpoint" });
            }

            rule.IsActive = !rule.IsActive;
            await ruleRepo.UpdateAsync(rule);
            await ruleRepo.SaveChangesAsync();
            await cache.ReloadRulesForEndpointAsync(endpointId);

            return Results.Ok(rule);
        })
        .WithName("ToggleRule")
        .WithOpenApi();
    }
}
