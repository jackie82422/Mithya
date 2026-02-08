using MockServer.Core.Entities;
using MockServer.Core.ValueObjects;

namespace MockServer.Core.Interfaces;

public interface IProtocolHandler
{
    ValidationResult ValidateEndpoint(MockEndpoint endpoint);
    ValidationResult ValidateRule(MockRule rule, MockEndpoint endpoint);
    ProtocolSchema GetSchema();
}
