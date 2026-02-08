using System.Xml;
using MockServer.Core.Enums;
using MockServer.Core.Interfaces;
using Newtonsoft.Json.Linq;

namespace MockServer.Infrastructure.MockEngine;

public class MatchEngine : IMatchEngine
{
    private readonly IMockRuleCache _cache;

    public MatchEngine(IMockRuleCache cache)
    {
        _cache = cache;
    }

    public Task<MatchResult?> FindMatchAsync(MockRequestContext context)
    {
        var endpoints = _cache.GetAllEndpoints();

        foreach (var endpoint in endpoints)
        {
            if (!endpoint.IsActive)
                continue;

            if (!string.Equals(endpoint.HttpMethod, context.Method, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!PathMatcher.IsMatch(endpoint.Path, context.Path))
                continue;

            // Extract path parameters for this endpoint
            var pathParams = PathMatcher.ExtractPathParams(endpoint.Path, context.Path);

            // Try rules in priority order (lower number = higher priority)
            foreach (var rule in endpoint.Rules.OrderBy(r => r.Priority))
            {
                if (EvaluateAllConditions(rule.Conditions, context, endpoint, pathParams))
                {
                    return Task.FromResult<MatchResult?>(new MatchResult
                    {
                        Endpoint = endpoint,
                        Rule = rule,
                        IsDefaultResponse = false
                    });
                }
            }

            // Fall back to default response
            if (!string.IsNullOrEmpty(endpoint.DefaultResponse))
            {
                return Task.FromResult<MatchResult?>(new MatchResult
                {
                    Endpoint = endpoint,
                    Rule = null,
                    IsDefaultResponse = true
                });
            }
        }

        return Task.FromResult<MatchResult?>(null);
    }

    private bool EvaluateAllConditions(
        List<Core.Entities.MatchCondition> conditions,
        MockRequestContext context,
        Core.ValueObjects.CachedEndpoint endpoint,
        Dictionary<string, string> pathParams)
    {
        if (conditions.Count == 0)
            return true;

        // AND logic: all conditions must match
        foreach (var condition in conditions)
        {
            var actual = ExtractValue(condition.SourceType, condition.FieldPath, context, endpoint, pathParams);

            if (!OperatorEvaluator.Evaluate(condition.Operator, actual, condition.Value))
                return false;
        }

        return true;
    }

    private string? ExtractValue(
        FieldSourceType sourceType,
        string fieldPath,
        MockRequestContext context,
        Core.ValueObjects.CachedEndpoint endpoint,
        Dictionary<string, string> pathParams)
    {
        return sourceType switch
        {
            FieldSourceType.Body => ExtractBodyValue(fieldPath, context.Body, endpoint.Protocol),
            FieldSourceType.Header => ExtractHeaderValue(fieldPath, context.Headers),
            FieldSourceType.Query => ExtractQueryValue(fieldPath, context.QueryParams),
            FieldSourceType.Path => ExtractPathValue(fieldPath, pathParams),
            _ => null
        };
    }

    private string? ExtractBodyValue(string fieldPath, string? body, ProtocolType protocol)
    {
        if (string.IsNullOrEmpty(body))
            return null;

        try
        {
            if (protocol == ProtocolType.SOAP)
            {
                var doc = new XmlDocument();
                doc.LoadXml(body);
                var node = doc.SelectSingleNode(fieldPath);
                return node?.InnerText;
            }
            else
            {
                // REST: use JsonPath via Newtonsoft.Json
                var token = JToken.Parse(body);
                var selected = token.SelectToken(fieldPath);
                return selected?.ToString();
            }
        }
        catch
        {
            return null;
        }
    }

    private string? ExtractHeaderValue(string fieldPath, Dictionary<string, string> headers)
    {
        // Case-insensitive header lookup
        var key = headers.Keys.FirstOrDefault(k => string.Equals(k, fieldPath, StringComparison.OrdinalIgnoreCase));
        return key != null ? headers[key] : null;
    }

    private string? ExtractQueryValue(string fieldPath, Dictionary<string, string> queryParams)
    {
        var key = queryParams.Keys.FirstOrDefault(k => string.Equals(k, fieldPath, StringComparison.OrdinalIgnoreCase));
        return key != null ? queryParams[key] : null;
    }

    private string? ExtractPathValue(string fieldPath, Dictionary<string, string> pathParams)
    {
        // fieldPath could be "{id}" or just "id"
        var paramName = fieldPath.Trim('{', '}');
        var key = pathParams.Keys.FirstOrDefault(k => string.Equals(k, paramName, StringComparison.OrdinalIgnoreCase));
        return key != null ? pathParams[key] : null;
    }
}
