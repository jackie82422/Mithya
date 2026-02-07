using MockServer.Core.Entities;
using MockServer.Core.ValueObjects;
using WireMock.Admin.Mappings;

namespace MockServer.Core.Interfaces;

public interface IProtocolHandler
{
    MappingModel ToWireMockMapping(MockRule rule, MockEndpoint endpoint);
    ValidationResult ValidateEndpoint(MockEndpoint endpoint);
    ValidationResult ValidateRule(MockRule rule, MockEndpoint endpoint);
    ProtocolSchema GetSchema();
}
