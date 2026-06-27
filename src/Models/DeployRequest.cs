namespace Api.Models;

public sealed class DeployRequest
{
    public string? Environment { get; set; }
    public string? Tag { get; set; }
}
