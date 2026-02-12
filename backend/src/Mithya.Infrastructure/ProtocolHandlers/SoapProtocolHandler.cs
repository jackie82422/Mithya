using Newtonsoft.Json;
using Mithya.Core.Entities;
using Mithya.Core.Interfaces;
using Mithya.Core.ValueObjects;
using CoreEnums = Mithya.Core.Enums;

namespace Mithya.Infrastructure.ProtocolHandlers;

public class SoapProtocolHandler : IProtocolHandler
{
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
            if (conditions != null)
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
