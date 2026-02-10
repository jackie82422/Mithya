using System.Xml;
using Mithya.Core.Enums;
using Mithya.Core.Interfaces;
using Mithya.Core.ValueObjects;
using Newtonsoft.Json.Linq;

namespace Mithya.Infrastructure.MockEngine;

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
                if (EvaluateAllConditions(rule, context, endpoint, pathParams))
                {
                    return Task.FromResult<MatchResult?>(new MatchResult
                    {
                        Endpoint = endpoint,
                        Rule = rule,
                        IsDefaultResponse = false,
                        PathParams = pathParams
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
                    IsDefaultResponse = true,
                    PathParams = pathParams
                });
            }
        }

        return Task.FromResult<MatchResult?>(null);
    }

    private bool EvaluateAllConditions(
        CachedRule rule,
        MockRequestContext context,
        CachedEndpoint endpoint,
        Dictionary<string, string> pathParams)
    {
        if (rule.Conditions.Count == 0)
            return true;

        if (rule.LogicMode == LogicMode.OR)
        {
            return rule.Conditions.Any(c =>
            {
                var actual = ExtractValue(c.SourceType, c.FieldPath, context, endpoint, pathParams);
                return OperatorEvaluator.Evaluate(c.Operator, actual, c.Value);
            });
        }

        // AND logic (default): all conditions must match
        return rule.Conditions.All(c =>
        {
            var actual = ExtractValue(c.SourceType, c.FieldPath, context, endpoint, pathParams);
            return OperatorEvaluator.Evaluate(c.Operator, actual, c.Value);
        });
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
