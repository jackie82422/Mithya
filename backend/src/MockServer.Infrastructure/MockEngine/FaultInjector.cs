using Microsoft.AspNetCore.Http;
using MockServer.Core.Enums;
using MockServer.Core.ValueObjects;
using Newtonsoft.Json;

namespace MockServer.Infrastructure.MockEngine;

public interface IFaultInjector
{
    Task<bool> ApplyFaultAsync(HttpContext httpContext, CachedRule rule);
}

public class FaultInjector : IFaultInjector
{
    public async Task<bool> ApplyFaultAsync(HttpContext httpContext, CachedRule rule)
    {
        switch (rule.FaultType)
        {
            case FaultType.None:
            case FaultType.FixedDelay:
                return false;

            case FaultType.RandomDelay:
                var randomConfig = DeserializeConfig<RandomDelayConfig>(rule.FaultConfig);
                var delay = Random.Shared.Next(
                    randomConfig?.MinDelayMs ?? 100,
                    (randomConfig?.MaxDelayMs ?? 5000) + 1);
                await Task.Delay(delay);
                return false;

            case FaultType.ConnectionReset:
                httpContext.Abort();
                return true;

            case FaultType.EmptyResponse:
                var emptyConfig = DeserializeConfig<EmptyResponseConfig>(rule.FaultConfig);
                httpContext.Response.StatusCode = emptyConfig?.StatusCode ?? 503;
                return true;

            case FaultType.MalformedResponse:
                var malConfig = DeserializeConfig<MalformedConfig>(rule.FaultConfig);
                var bytes = new byte[malConfig?.ByteCount ?? 256];
                Random.Shared.NextBytes(bytes);
                httpContext.Response.ContentType = "application/octet-stream";
                await httpContext.Response.Body.WriteAsync(bytes);
                return true;

            case FaultType.Timeout:
                var timeoutConfig = DeserializeConfig<TimeoutConfig>(rule.FaultConfig);
                await Task.Delay(timeoutConfig?.TimeoutMs ?? 30000);
                httpContext.Abort();
                return true;

            default:
                return false;
        }
    }

    private static T? DeserializeConfig<T>(string? json) where T : class
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
        catch
        {
            return null;
        }
    }
}

public class RandomDelayConfig
{
    public int MinDelayMs { get; set; } = 100;
    public int MaxDelayMs { get; set; } = 5000;
}

public class EmptyResponseConfig
{
    public int StatusCode { get; set; } = 503;
}

public class MalformedConfig
{
    public int ByteCount { get; set; } = 256;
}

public class TimeoutConfig
{
    public int TimeoutMs { get; set; } = 30000;
}
