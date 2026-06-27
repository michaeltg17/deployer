namespace Api.Models;

public sealed class DeployRequest
{
    public string? Project { get; set; }
    public string? Environment { get; set; }
    public string? Tag { get; set; }

    public override string ToString() =>
        $"DeployRequest(Project={Project}, Environment={Environment}, Tag={Tag})";
}