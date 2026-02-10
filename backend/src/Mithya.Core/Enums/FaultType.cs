namespace Mithya.Core.Enums;

public enum FaultType
{
    None = 0,
    FixedDelay = 1,
    RandomDelay = 2,
    ConnectionReset = 3,
    EmptyResponse = 4,
    MalformedResponse = 5,
    Timeout = 6
}
