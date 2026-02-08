using Microsoft.Extensions.DependencyInjection;
using MockServer.Core.Entities;
using MockServer.Core.Enums;
using MockServer.Core.Interfaces;
using Newtonsoft.Json;

namespace MockServer.Infrastructure.MockEngine;

public interface IRecordingService
{
    Task RecordAsync(MockRequestContext request, ProxyResponse response, Guid? endpointId);
}

public class RecordingService : IRecordingService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMockRuleCache _cache;

    public RecordingService(IServiceScopeFactory scopeFactory, IMockRuleCache cache)
    {
        _scopeFactory = scopeFactory;
        _cache = cache;
    }

    public async Task RecordAsync(MockRequestContext request, ProxyResponse response, Guid? endpointId)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var endpointRepo = scope.ServiceProvider.GetRequiredService<IEndpointRepository>();
            var ruleRepo = scope.ServiceProvider.GetRequiredService<IRuleRepository>();

            if (endpointId == null)
            {
                var endpoint = new MockEndpoint
                {
                    Name = $"Recorded: {request.Method} {request.Path}",
                    ServiceName = "recorded",
                    Protocol = ProtocolType.REST,
                    Path = request.Path,
                    HttpMethod = request.Method,
                    IsActive = true,
                };
                await endpointRepo.AddAsync(endpoint);
                await endpointRepo.SaveChangesAsync();
                endpointId = endpoint.Id;
                await _cache.ReloadEndpointAsync(endpoint.Id);
            }

            var rule = new MockRule
            {
                EndpointId = endpointId.Value,
                RuleName = $"Recorded at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
                Priority = 100,
                MatchConditions = "[]",
                ResponseStatusCode = response.StatusCode,
                ResponseBody = response.Body,
                ResponseHeaders = response.Headers.Count > 0
                    ? JsonConvert.SerializeObject(response.Headers)
                    : null,
                DelayMs = 0,
                IsActive = true,
            };
            await ruleRepo.AddAsync(rule);
            await ruleRepo.SaveChangesAsync();
            await _cache.ReloadRulesForEndpointAsync(endpointId.Value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Recording error: {ex.Message}");
        }
    }
}
