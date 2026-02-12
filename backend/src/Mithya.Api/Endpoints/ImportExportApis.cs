using Mithya.Api.DTOs.Requests;
using Mithya.Core.Entities;
using Mithya.Core.Enums;
using Mithya.Core.Interfaces;
using Mithya.Infrastructure.MockEngine;
using Newtonsoft.Json.Linq;

namespace Mithya.Api.Endpoints;

public static class ImportExportApis
{
    public static void MapImportExportApis(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/api").WithTags("Import/Export");

        // GET /admin/api/export
        group.MapGet("/export", async (IEndpointRepository repo, IServiceProxyRepository proxyRepo) =>
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

            var serviceProxies = await proxyRepo.GetAllAsync();
            var exportProxies = serviceProxies.Select(p => new
            {
                p.ServiceName,
                p.TargetBaseUrl,
                p.IsActive,
                p.IsRecording,
                p.ForwardHeaders,
                p.AdditionalHeaders,
                p.TimeoutMs,
                p.StripPathPrefix,
                p.FallbackEnabled
            });

            return Results.Ok(new { version = "1.1", exportedAt = DateTime.UtcNow, endpoints = exportData, serviceProxies = exportProxies });
        })
        .WithName("ExportAll")
        .WithOpenApi();

        // POST /admin/api/import/json
        group.MapPost("/import/json", async (
            ImportJsonRequest request,
            IEndpointRepository endpointRepo,
            IRuleRepository ruleRepo,
            IServiceProxyRepository proxyRepo,
            IMockRuleCache cache,
            IServiceProxyCache proxyCache) =>
        {
            if ((request.Endpoints == null || request.Endpoints.Count == 0) &&
                (request.ServiceProxies == null || request.ServiceProxies.Count == 0))
                return Results.BadRequest(new { error = "No endpoints or service proxies to import" });

            var existingEndpoints = await endpointRepo.GetAllAsync();
            var imported = new List<object>();
            var skipped = new List<object>();

            if (request.Endpoints != null)
            {
                foreach (var ep in request.Endpoints)
                {
                    var method = ep.HttpMethod.ToUpper();

                    // Check for duplicate path+method
                    var duplicate = existingEndpoints.FirstOrDefault(e =>
                        e.Path == ep.Path &&
                        e.HttpMethod.Equals(method, StringComparison.OrdinalIgnoreCase));

                    if (duplicate != null)
                    {
                        skipped.Add(new { ep.Name, ep.Path, httpMethod = method, reason = "duplicate", existingEndpointId = duplicate.Id });
                        continue;
                    }

                    var endpoint = new MockEndpoint
                    {
                        Name = ep.Name,
                        ServiceName = ep.ServiceName,
                        Protocol = (ProtocolType)ep.Protocol,
                        Path = ep.Path,
                        HttpMethod = method,
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

                    // Track newly added for duplicate detection within same batch
                    existingEndpoints = existingEndpoints.Append(endpoint);
                }
            }

            // Import service proxies
            var importedProxies = new List<object>();
            var skippedProxies = new List<object>();

            if (request.ServiceProxies != null)
            {
                foreach (var sp in request.ServiceProxies)
                {
                    var existing = await proxyRepo.GetByServiceNameAsync(sp.ServiceName);
                    if (existing != null)
                    {
                        // Update existing
                        existing.TargetBaseUrl = sp.TargetBaseUrl;
                        existing.IsActive = sp.IsActive;
                        existing.IsRecording = sp.IsRecording;
                        existing.ForwardHeaders = sp.ForwardHeaders;
                        existing.AdditionalHeaders = sp.AdditionalHeaders;
                        existing.TimeoutMs = sp.TimeoutMs;
                        existing.StripPathPrefix = sp.StripPathPrefix;
                        existing.FallbackEnabled = sp.FallbackEnabled;
                        await proxyRepo.UpdateAsync(existing);
                        importedProxies.Add(new { existing.Id, sp.ServiceName, action = "updated" });
                    }
                    else
                    {
                        // Create new
                        var proxy = new ServiceProxy
                        {
                            ServiceName = sp.ServiceName,
                            TargetBaseUrl = sp.TargetBaseUrl,
                            IsActive = sp.IsActive,
                            IsRecording = sp.IsRecording,
                            ForwardHeaders = sp.ForwardHeaders,
                            AdditionalHeaders = sp.AdditionalHeaders,
                            TimeoutMs = sp.TimeoutMs,
                            StripPathPrefix = sp.StripPathPrefix,
                            FallbackEnabled = sp.FallbackEnabled
                        };
                        await proxyRepo.AddAsync(proxy);
                        importedProxies.Add(new { proxy.Id, sp.ServiceName, action = "created" });
                    }
                }
                await proxyRepo.SaveChangesAsync();
                await proxyCache.ReloadAsync();
            }

            return Results.Ok(new
            {
                imported = imported.Count,
                skipped = skipped.Count,
                endpoints = imported,
                duplicates = skipped,
                serviceProxies = new { imported = importedProxies.Count, details = importedProxies }
            });
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

                var existingEndpoints = await endpointRepo.GetAllAsync();
                var imported = new List<object>();
                var skipped = new List<object>();
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

                        // Check for duplicate path+method
                        var duplicate = existingEndpoints.FirstOrDefault(e =>
                            e.Path == pathProp.Name &&
                            e.HttpMethod.Equals(method, StringComparison.OrdinalIgnoreCase));

                        if (duplicate != null)
                        {
                            skipped.Add(new { path = pathProp.Name, httpMethod = method, reason = "duplicate", existingEndpointId = duplicate.Id });
                            continue;
                        }

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

                        existingEndpoints = existingEndpoints.Append(endpoint);
                    }
                }

                await endpointRepo.SaveChangesAsync();
                await cache.LoadAllAsync();

                return Results.Ok(new { imported = imported.Count, skipped = skipped.Count, endpoints = imported, duplicates = skipped });
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
