using Newtonsoft.Json;
using MockServer.Core.Entities;
using MockServer.Core.Interfaces;
using MockServer.Core.ValueObjects;
using CoreEnums = MockServer.Core.Enums;

namespace MockServer.Infrastructure.ProtocolHandlers;

public class RestProtocolHandler : IProtocolHandler
{
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

                    if (condition.SourceType == CoreEnums.FieldSourceType.Body &&
                        !condition.FieldPath.StartsWith("$") )
                        errors.Add("Body FieldPath must be a valid JsonPath (starts with $)");
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
