using MockServer.Api.DTOs.Requests;
using MockServer.Core.Entities;
using MockServer.Core.Enums;
using MockServer.Core.Interfaces;
using Newtonsoft.Json.Linq;

namespace MockServer.Api.Endpoints;

public static class ImportExportApis
{
    public static void MapImportExportApis(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/api").WithTags("Import/Export");

        // GET /admin/api/export
        group.MapGet("/export", async (IEndpointRepository repo) =>
        {
            var endpoints = await repo.GetAllAsync();
            var exportData = endpoints.Select(e => new
            {
                e.Name,
                e.ServiceName,
                Protocol = (int)e.Protocol,
                e.Path,
                e.HttpMethod,
                e.DefaultResponse,
                e.DefaultStatusCode,
                e.ProtocolSettings,
                e.IsActive,
                Rules = e.Rules.Select(r => new
                {
                    r.RuleName,
                    r.Priority,
                    r.MatchConditions,
                    r.ResponseStatusCode,
                    r.ResponseBody,
                    r.ResponseHeaders,
                    r.DelayMs,
                    r.IsTemplate,
                    r.IsResponseHeadersTemplate,
                    FaultType = (int)r.FaultType,
                    r.FaultConfig,
                    LogicMode = (int)r.LogicMode,
                    r.IsActive
                })
            });

            return Results.Ok(new { version = "1.0", exportedAt = DateTime.UtcNow, endpoints = exportData });
        })
        .WithName("ExportAll")
        .WithOpenApi();

        // POST /admin/api/import/json
        group.MapPost("/import/json", async (
            ImportJsonRequest request,
            IEndpointRepository endpointRepo,
            IRuleRepository ruleRepo,
            IMockRuleCache cache) =>
        {
            if (request.Endpoints == null || request.Endpoints.Count == 0)
                return Results.BadRequest(new { error = "No endpoints to import" });

            var imported = new List<object>();

            foreach (var ep in request.Endpoints)
            {
                var endpoint = new MockEndpoint
                {
                    Name = ep.Name,
                    ServiceName = ep.ServiceName,
                    Protocol = (ProtocolType)ep.Protocol,
                    Path = ep.Path,
                    HttpMethod = ep.HttpMethod.ToUpper(),
                    DefaultResponse = ep.DefaultResponse,
                    DefaultStatusCode = ep.DefaultStatusCode,
                    ProtocolSettings = ep.ProtocolSettings,
                    IsActive = ep.IsActive
                };

                await endpointRepo.AddAsync(endpoint);
                await endpointRepo.SaveChangesAsync();

                if (ep.Rules != null)
                {
                    foreach (var r in ep.Rules)
                    {
                        var rule = new MockRule
                        {
                            EndpointId = endpoint.Id,
                            RuleName = r.RuleName,
                            Priority = r.Priority,
                            MatchConditions = r.MatchConditions,
                            ResponseStatusCode = r.ResponseStatusCode,
                            ResponseBody = r.ResponseBody,
                            ResponseHeaders = r.ResponseHeaders,
                            DelayMs = r.DelayMs,
                            IsTemplate = r.IsTemplate,
                            IsResponseHeadersTemplate = r.IsResponseHeadersTemplate,
                            FaultType = (FaultType)r.FaultType,
                            FaultConfig = r.FaultConfig,
                            LogicMode = (LogicMode)r.LogicMode,
                            IsActive = r.IsActive
                        };

                        await ruleRepo.AddAsync(rule);
                    }
                    await ruleRepo.SaveChangesAsync();
                }

                await cache.ReloadEndpointAsync(endpoint.Id);
                imported.Add(new { endpoint.Id, endpoint.Name, endpoint.Path, rulesCount = ep.Rules?.Count ?? 0 });
            }

            return Results.Ok(new { imported = imported.Count, endpoints = imported });
        })
        .WithName("ImportJson")
        .WithOpenApi();

        // POST /admin/api/import/openapi
        group.MapPost("/import/openapi", async (
            ImportOpenApiRequest request,
            IEndpointRepository endpointRepo,
            IMockRuleCache cache) =>
        {
            if (string.IsNullOrWhiteSpace(request.Spec))
                return Results.BadRequest(new { error = "OpenAPI spec is required" });

            try
            {
                var spec = JObject.Parse(request.Spec);
                var paths = spec["paths"] as JObject;
                if (paths == null)
                    return Results.BadRequest(new { error = "Invalid OpenAPI spec: missing 'paths'" });

                var imported = new List<object>();
                var validMethods = new[] { "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS" };

                foreach (var pathProp in paths.Properties())
                {
                    var pathValue = pathProp.Value as JObject;
                    if (pathValue == null) continue;

                    foreach (var methodProp in pathValue.Properties())
                    {
                        var method = methodProp.Name.ToUpper();
                        if (!validMethods.Contains(method))
                            continue;

                        var operation = methodProp.Value as JObject;
                        var summary = operation?["summary"]?.ToString() ?? $"{method} {pathProp.Name}";

                        var endpoint = new MockEndpoint
                        {
                            Name = summary,
                            ServiceName = spec["info"]?["title"]?.ToString() ?? "Imported",
                            Protocol = ProtocolType.REST,
                            Path = pathProp.Name,
                            HttpMethod = method,
                            IsActive = true
                        };

                        // Set default response from first 2xx response
                        var responses = operation?["responses"] as JObject;
                        if (responses != null)
                        {
                            var successResponse = responses.Properties()
                                .FirstOrDefault(p => p.Name.StartsWith("2"));

                            if (successResponse != null)
                            {
                                endpoint.DefaultStatusCode = int.TryParse(successResponse.Name, out var code) ? code : 200;
                                var content = successResponse.Value?["content"]?["application/json"];
                                var example = content?["example"]?.ToString();
                                if (example != null)
                                    endpoint.DefaultResponse = example;
                            }
                        }

                        await endpointRepo.AddAsync(endpoint);
                        imported.Add(new { endpoint.Id, endpoint.Name, endpoint.Path, endpoint.HttpMethod });
                    }
                }

                await endpointRepo.SaveChangesAsync();
                await cache.LoadAllAsync();

                return Results.Ok(new { imported = imported.Count, endpoints = imported });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = $"Failed to parse OpenAPI spec: {ex.Message}" });
            }
        })
        .WithName("ImportOpenApi")
        .WithOpenApi();
    }
}
