namespace Mithya.Api.DTOs.Requests;

public class SetDefaultResponseRequest
{
    public int StatusCode { get; set; } = 200;
    public string ResponseBody { get; set; } = string.Empty;
}
