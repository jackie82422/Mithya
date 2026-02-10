namespace MockServer.Api.DTOs.Requests;

public class ImportJsonRequest
{
    public List<ImportEndpointData> Endpoints { get; set; } = new();
    public List<ImportServiceProxyData>? ServiceProxies { get; set; }
}

public class ImportServiceProxyData
{
    public string ServiceName { get; set; } = string.Empty;
    public string TargetBaseUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsRecording { get; set; }
    public bool ForwardHeaders { get; set; } = true;
    public string? AdditionalHeaders { get; set; }
    public int TimeoutMs { get; set; } = 10000;
    public string? StripPathPrefix { get; set; }
    public bool FallbackEnabled { get; set; } = true;
}

public class ImportEndpointData
{
    public string Name { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public int Protocol { get; set; }
    public string Path { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string? DefaultResponse { get; set; }
    public int? DefaultStatusCode { get; set; }
    public string? ProtocolSettings { get; set; }
    public bool IsActive { get; set; } = true;
    public List<ImportRuleData>? Rules { get; set; }
}

public class ImportRuleData
{
    public string RuleName { get; set; } = string.Empty;
    public int Priority { get; set; } = 100;
    public string MatchConditions { get; set; } = "[]";
    public int ResponseStatusCode { get; set; } = 200;
    public string ResponseBody { get; set; } = string.Empty;
    public string? ResponseHeaders { get; set; }
    public int DelayMs { get; set; }
    public bool IsTemplate { get; set; }
    public bool IsResponseHeadersTemplate { get; set; }
    public int FaultType { get; set; }
    public string? FaultConfig { get; set; }
    public int LogicMode { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ImportOpenApiRequest
{
    public string Spec { get; set; } = string.Empty;
}
