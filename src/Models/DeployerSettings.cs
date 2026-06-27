using System.ComponentModel.DataAnnotations;

namespace Api.Models;

public sealed class DeployerSettings
{
    [Required]
    public required string GhcrUser { get; set; }

    [Required]
    public required string GhcrToken { get; set; }

    [Required]
    public required string ImageRepo { get; set; }

    public string? KeePassDbPath { get; set; }

    public string? KeePassDbPassword { get; set; }
}
