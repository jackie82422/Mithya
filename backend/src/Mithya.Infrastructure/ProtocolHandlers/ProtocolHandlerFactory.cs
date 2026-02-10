using Mithya.Core.Interfaces;
using Mithya.Core.ValueObjects;
using CoreEnums = Mithya.Core.Enums;

namespace Mithya.Infrastructure.ProtocolHandlers;

public class ProtocolHandlerFactory
{
    private readonly Dictionary<CoreEnums.ProtocolType, IProtocolHandler> _handlers;

    public ProtocolHandlerFactory()
    {
        _handlers = new Dictionary<CoreEnums.ProtocolType, IProtocolHandler>
        {
            { CoreEnums.ProtocolType.REST, new RestProtocolHandler() },
            { CoreEnums.ProtocolType.SOAP, new SoapProtocolHandler() }
        };
    }

    public IProtocolHandler GetHandler(CoreEnums.ProtocolType protocol)
    {
        if (_handlers.TryGetValue(protocol, out var handler))
        {
            return handler;
        }

        throw new NotSupportedException($"Protocol {protocol} is not supported");
    }

    public List<ProtocolSchema> GetAllSchemas()
    {
        return _handlers.Values.Select(h => h.GetSchema()).ToList();
    }
}
