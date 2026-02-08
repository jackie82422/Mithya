using System.Text.RegularExpressions;
using Newtonsoft.Json;
using MockServer.Core.Entities;
using MockServer.Core.Interfaces;
using MockServer.Core.ValueObjects;
using WireMock.Admin.Mappings;
using CoreEnums = MockServer.Core.Enums;

namespace MockServer.Infrastructure.ProtocolHandlers;

public class RestProtocolHandler : IProtocolHandler
{
    public MappingModel ToWireMockMapping(MockRule rule, MockEndpoint endpoint)
    {
        var conditions = JsonConvert.DeserializeObject<List<MatchCondition>>(rule.MatchConditions)
                         ?? new List<MatchCondition>();

        var requestModel = new RequestModel
        {
            Path = new PathModel
            {
                Matchers = new[]
                {
                    new MatcherModel
                    {
                        Name = "WildcardMatcher",
                        Pattern = ConvertPathToPattern(endpoint.Path)
                    }
                }
            },
            Methods = new[] { endpoint.HttpMethod }
        };

        // Process conditions
        foreach (var condition in conditions)
        {
            switch (condition.SourceType)
            {
                case CoreEnums.FieldSourceType.Body:
                    AddBodyMatcher(requestModel, condition);
                    break;
                case CoreEnums.FieldSourceType.Header:
                    AddHeaderMatcher(requestModel, condition);
                    break;
                case CoreEnums.FieldSourceType.Query:
                    AddQueryMatcher(requestModel, condition);
                    break;
                case CoreEnums.FieldSourceType.Path:
                    // Path parameters are handled in the path pattern
                    break;
            }
        }

        var responseModel = new ResponseModel
        {
            StatusCode = rule.ResponseStatusCode,
            Body = rule.ResponseBody
        };

        if (!string.IsNullOrEmpty(rule.ResponseHeaders))
        {
            responseModel.Headers = JsonConvert.DeserializeObject<Dictionary<string, object>>(rule.ResponseHeaders);
        }

        if (rule.DelayMs > 0)
        {
            responseModel.Delay = rule.DelayMs;
        }

        return new MappingModel
        {
            Guid = rule.Id,
            Priority = rule.Priority,
            Request = requestModel,
            Response = responseModel
        };
    }

    private string ConvertPathToPattern(string path)
    {
        // Convert path parameters like /api/users/{id} to /api/users/*
        return Regex.Replace(path, @"\{[^}]+\}", "*");
    }

    private void AddBodyMatcher(RequestModel request, MatchCondition condition)
    {
        var matcher = condition.Operator switch
        {
            CoreEnums.MatchOperator.Equals => new MatcherModel
            {
                Name = "JsonPathMatcher",
                Pattern = condition.FieldPath,
                Patterns = new[] { condition.Value }
            },
            CoreEnums.MatchOperator.Contains => new MatcherModel
            {
                Name = "JsonPathMatcher",
                Pattern = condition.FieldPath,
                Patterns = new[] { $"*{condition.Value}*" }
            },
            CoreEnums.MatchOperator.Regex => new MatcherModel
            {
                Name = "JsonPathMatcher",
                Pattern = condition.FieldPath,
                Patterns = new[] { condition.Value }
            },
            _ => throw new NotSupportedException($"Operator {condition.Operator} not supported for Body")
        };

        request.Body = new BodyModel
        {
            Matcher = matcher
        };
    }

    private void AddHeaderMatcher(RequestModel request, MatchCondition condition)
    {
        request.Headers ??= new List<HeaderModel>();

        var matcher = condition.Operator switch
        {
            CoreEnums.MatchOperator.Equals => new MatcherModel
            {
                Name = "ExactMatcher",
                Pattern = condition.Value
            },
            CoreEnums.MatchOperator.Contains => new MatcherModel
            {
                Name = "WildcardMatcher",
                Pattern = $"*{condition.Value}*"
            },
            CoreEnums.MatchOperator.Regex => new MatcherModel
            {
                Name = "RegexMatcher",
                Pattern = condition.Value
            },
            _ => throw new NotSupportedException($"Operator {condition.Operator} not supported for Header")
        };

        request.Headers.Add(new HeaderModel
        {
            Name = condition.FieldPath,
            Matchers = new[] { matcher }
        });
    }

    private void AddQueryMatcher(RequestModel request, MatchCondition condition)
    {
        request.Params ??= new List<ParamModel>();

        var matcher = condition.Operator switch
        {
            CoreEnums.MatchOperator.Equals => new MatcherModel
            {
                Name = "ExactMatcher",
                Pattern = condition.Value
            },
            CoreEnums.MatchOperator.Contains => new MatcherModel
            {
                Name = "WildcardMatcher",
                Pattern = $"*{condition.Value}*"
            },
            CoreEnums.MatchOperator.Regex => new MatcherModel
            {
                Name = "RegexMatcher",
                Pattern = condition.Value
            },
            _ => throw new NotSupportedException($"Operator {condition.Operator} not supported for Query")
        };

        request.Params.Add(new ParamModel
        {
            Name = condition.FieldPath,
            Matchers = new[] { matcher }
        });
    }

    public ValidationResult ValidateEndpoint(MockEndpoint endpoint)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(endpoint.Path))
            errors.Add("Path is required");

        if (string.IsNullOrWhiteSpace(endpoint.HttpMethod))
            errors.Add("HttpMethod is required");

        var validMethods = new[] { "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS" };
        if (!validMethods.Contains(endpoint.HttpMethod.ToUpper()))
            errors.Add($"HttpMethod must be one of: {string.Join(", ", validMethods)}");

        return errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors.ToArray());
    }

    public ValidationResult ValidateRule(MockRule rule, MockEndpoint endpoint)
    {
        var errors = new List<string>();

        try
        {
            var conditions = JsonConvert.DeserializeObject<List<MatchCondition>>(rule.MatchConditions);
            if (conditions != null)
            {
                foreach (var condition in conditions)
                {
                    if (string.IsNullOrWhiteSpace(condition.FieldPath))
                        errors.Add("FieldPath is required for all conditions");

                    if (condition.SourceType == CoreEnums.FieldSourceType.Body && !condition.FieldPath.StartsWith("$."))
                        errors.Add("Body FieldPath must be a valid JsonPath (starts with $.)");
                }
            }
        }
        catch (JsonException)
        {
            errors.Add("MatchConditions must be valid JSON");
        }

        if (rule.ResponseStatusCode < 100 || rule.ResponseStatusCode > 599)
            errors.Add("ResponseStatusCode must be between 100 and 599");

        return errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors.ToArray());
    }

    public ProtocolSchema GetSchema()
    {
        return new ProtocolSchema
        {
            Protocol = CoreEnums.ProtocolType.REST,
            Name = "REST/JSON",
            Description = "RESTful API with JSON payload matching using JsonPath",
            SupportedSources = new List<CoreEnums.FieldSourceType>
            {
                CoreEnums.FieldSourceType.Body,
                CoreEnums.FieldSourceType.Header,
                CoreEnums.FieldSourceType.Query,
                CoreEnums.FieldSourceType.Path
            },
            SupportedOperators = new List<CoreEnums.MatchOperator>
            {
                CoreEnums.MatchOperator.Equals,
                CoreEnums.MatchOperator.Contains,
                CoreEnums.MatchOperator.Regex,
                CoreEnums.MatchOperator.StartsWith,
                CoreEnums.MatchOperator.EndsWith
            },
            ExampleFieldPaths = new Dictionary<string, string>
            {
                { "Body", "$.userId" },
                { "Header", "X-User-Id" },
                { "Query", "accountId" },
                { "Path", "{id}" }
            }
        };
    }
}
