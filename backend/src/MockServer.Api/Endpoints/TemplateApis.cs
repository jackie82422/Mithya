using MockServer.Core.Interfaces;

namespace MockServer.Api.Endpoints;

public static class TemplateApis
{
    public static void MapTemplateApis(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/api/templates").WithTags("Templates");

        group.MapPost("/preview", (TemplatePreviewRequest request, ITemplateEngine templateEngine) =>
        {
            try
            {
                var context = new TemplateContext
                {
                    Request = new TemplateRequestData
                    {
                        Method = request.MockRequest?.Method ?? "GET",
                        Path = request.MockRequest?.Path ?? "/",
                        Body = request.MockRequest?.Body,
                        Headers = request.MockRequest?.Headers ?? new(),
                        Query = request.MockRequest?.Query ?? new(),
                        PathParams = request.MockRequest?.PathParams ?? new()
                    }
                };

                var rendered = templateEngine.Render(request.Template ?? "", context);
                return Results.Ok(new { rendered, error = (string?)null });
            }
            catch (Exception ex)
            {
                return Results.Ok(new { rendered = (string?)null, error = ex.Message });
            }
        })
        .WithName("PreviewTemplate")
        .WithOpenApi();
    }
}

public class TemplatePreviewRequest
{
    public string? Template { get; set; }
    public TemplateMockRequest? MockRequest { get; set; }
}

public class TemplateMockRequest
{
    public string Method { get; set; } = "GET";
    public string Path { get; set; } = "/";
    public string? Body { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public Dictionary<string, string> Query { get; set; } = new();
    public Dictionary<string, string> PathParams { get; set; } = new();
}
