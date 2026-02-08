namespace MockServer.Core.Interfaces;

public class TemplateContext
{
    public TemplateRequestData Request { get; set; } = new();
}

public class TemplateRequestData
{
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? Body { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public Dictionary<string, string> Query { get; set; } = new();
    public Dictionary<string, string> PathParams { get; set; } = new();
}

public interface ITemplateEngine
{
    string Render(string template, TemplateContext context);
}
