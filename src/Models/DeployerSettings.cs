namespace Api.Models;

internal sealed class DeployerSettings
{
    public required string ImageRepo { get; set; }

    public required string KeePassDbPath { get; set; }

    public required string KeePassDbPassword { get; set; }

    public string ProjectsDir { get; set; } = "/projects";
}
