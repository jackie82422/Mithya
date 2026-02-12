using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Mithya.Api.DTOs.Requests;
using Mithya.Core.Entities;
using Mithya.Core.Enums;
using Mithya.Core.Interfaces;
using Mithya.Infrastructure.ProtocolHandlers;

namespace Mithya.Api.Endpoints;

public static class RuleManagementApis
{
    private static readonly Regex HtmlTagPattern = new(@"<[^>]+>", RegexOptions.Compiled);

    private static List<string> ValidateRuleRequest(CreateRuleRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.RuleName))
            errors.Add("RuleName is required");
        else if (request.RuleName.Length > 200)
            errors.Add("RuleName must be 200 characters or less");
        else if (HtmlTagPattern.IsMatch(request.RuleName))
            errors.Add("RuleName must not contain HTML tags");

        if (request.DelayMs < 0)
            errors.Add("DelayMs must be 0 or greater");

        if (!Enum.IsDefined(typeof(FaultType), request.FaultType))
            errors.Add($"FaultType must be one of: {string.Join(", ", Enum.GetValues<FaultType>().Select(f => $"{(int)f}({f})"))}");

        if (!Enum.IsDefined(typeof(LogicMode), request.LogicMode))
            errors.Add($"LogicMode must be one of: {string.Join(", ", Enum.GetValues<LogicMode>().Select(l => $"{(int)l}({l})"))}");

        if (request.StatusCode < 100 || request.StatusCode > 599)
            errors.Add("StatusCode must be between 100 and 599");

        return errors;
    }

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
            // Input validation
            var validationErrors = ValidateRuleRequest(request);
            if (validationErrors.Count > 0)
                return Results.BadRequest(new { errors = validationErrors });

            var endpoint = await endpointRepo.GetByIdAsync(endpointId);
            if (endpoint is null)
            {
                return Results.NotFound(new { error = "Endpoint not found" });
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
                LogicMode = request.LogicMode,
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
            // Input validation
            var validationErrors = ValidateRuleRequest(request);
            if (validationErrors.Count > 0)
                return Results.BadRequest(new { errors = validationErrors });

            // Validate endpoint exists
            var endpoint = await endpointRepo.GetByIdAsync(endpointId);
            if (endpoint is null)
            {
                return Results.NotFound(new { error = "Endpoint not found" });
            }

            // Validate rule exists and belongs to the endpoint
            var rule = await ruleRepo.GetByIdAsync(ruleId);
            if (rule is null || rule.EndpointId != endpointId)
            {
                return Results.NotFound(new { error = "Rule not found" });
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
            rule.LogicMode = request.LogicMode;

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
                return Results.NotFound(new { error = "Rule not found" });
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
                return Results.NotFound(new { error = "Endpoint not found" });
            }

            var rule = await ruleRepo.GetByIdAsync(ruleId);
            if (rule is null)
            {
                return Results.NotFound(new { error = "Rule not found" });
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
