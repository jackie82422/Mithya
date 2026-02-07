using Newtonsoft.Json;
using MockServer.Core.Entities;
using MockServer.Core.Interfaces;
using MockServer.Core.ValueObjects;
using WireMock.Admin.Mappings;
using CoreEnums = MockServer.Core.Enums;

namespace MockServer.Infrastructure.ProtocolHandlers;

public class SoapProtocolHandler : IProtocolHandler
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
                        Pattern = endpoint.Path
                    }
                }
            },
            Methods = new[] { endpoint.HttpMethod }
        };

        // Process conditions (SOAP uses Body with XPath)
        foreach (var condition in conditions)
        {
            if (condition.SourceType == CoreEnums.FieldSourceType.Body)
            {
                AddXPathMatcher(requestModel, condition);
            }
            else if (condition.SourceType == CoreEnums.FieldSourceType.Header)
            {
                AddHeaderMatcher(requestModel, condition);
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
        else
        {
            // Default SOAP headers
            responseModel.Headers = new Dictionary<string, object>
            {
                { "Content-Type", "text/xml; charset=utf-8" }
            };
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

    private void AddXPathMatcher(RequestModel request, MatchCondition condition)
    {
        var matcher = condition.Operator switch
        {
            CoreEnums.MatchOperator.Equals => new MatcherModel
            {
                Name = "XPathMatcher",
                Pattern = condition.FieldPath,
                Patterns = new[] { condition.Value }
            },
            CoreEnums.MatchOperator.Contains => new MatcherModel
            {
                Name = "XPathMatcher",
                Pattern = condition.FieldPath,
                Patterns = new[] { $"*{condition.Value}*" }
            },
            _ => throw new NotSupportedException($"Operator {condition.Operator} not supported for SOAP Body")
        };

        request.Body = new BodyModel
        {
            Matcher = matcher
        };
    }

    private void AddHeaderMatcher(RequestModel request, MatchCondition condition)
    {
        request.Headers ??= new List<HeaderModel>();

        var matcher = new MatcherModel
        {
            Name = "ExactMatcher",
            Pattern = condition.Value
        };

        request.Headers.Add(new HeaderModel
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

        if (endpoint.HttpMethod.ToUpper() != "POST")
            errors.Add("SOAP endpoints must use POST method");

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
            if (conditions == null || conditions.Count == 0)
            {
                errors.Add("At least one match condition is required");
            }
            else
            {
                foreach (var condition in conditions)
                {
                    if (string.IsNullOrWhiteSpace(condition.FieldPath))
                        errors.Add("FieldPath is required for all conditions");

                    if (condition.SourceType == CoreEnums.FieldSourceType.Body &&
                        !condition.FieldPath.StartsWith("/") &&
                        !condition.FieldPath.Contains("local-name()"))
                    {
                        errors.Add("SOAP Body FieldPath must be a valid XPath expression");
                    }
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
            Protocol = CoreEnums.ProtocolType.SOAP,
            Name = "SOAP/XML",
            Description = "SOAP API with XML payload matching using XPath",
            SupportedSources = new List<CoreEnums.FieldSourceType>
            {
                CoreEnums.FieldSourceType.Body,
                CoreEnums.FieldSourceType.Header
            },
            SupportedOperators = new List<CoreEnums.MatchOperator>
            {
                CoreEnums.MatchOperator.Equals,
                CoreEnums.MatchOperator.Contains
            },
            ExampleFieldPaths = new Dictionary<string, string>
            {
                { "Body", "//*[local-name()='userId']" },
                { "Header", "SOAPAction" }
            }
        };
    }
}
