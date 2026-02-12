using Mithya.Core.Entities;
using Mithya.Core.ValueObjects;

namespace Mithya.Core.Interfaces;

public interface IProtocolHandler
{
    ValidationResult ValidateEndpoint(MockEndpoint endpoint);
    ValidationResult ValidateRule(MockRule rule, MockEndpoint endpoint);
    ProtocolSchema GetSchema();
}
