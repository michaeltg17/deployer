using Api.Logging;
using Api.Models;
using Microsoft.Extensions.Options;

namespace Api.Services;

internal sealed class KeePassEnvService(
    ILogger<KeePassEnvService> logger,
    IOptions<DeployerSettings> settings,
    IProcessRunner processRunner)
{
    private readonly string dbPath = settings.Value.KeePassDbPath;
    private readonly string password = settings.Value.KeePassDbPassword;
    private readonly string projectsGroup = "projects";

    public async Task<Dictionary<string, string>> ExtractEnvVariables(string project, string environment)
    {
        var vars = new Dictionary<string, string>(StringComparer.Ordinal);

        var common = await ExtractAttachment(project, ".env").ConfigureAwait(false);
        if (!string.IsNullOrEmpty(common))
            ParseEnvContent(common, vars);

        var envSpecific = await ExtractAttachment(project, $".env.{environment}").ConfigureAwait(false);
        if (!string.IsNullOrEmpty(envSpecific))
            ParseEnvContent(envSpecific, vars);

        logger.LogEnvExtracted(project, environment, vars.Count);
        return vars;
    }

    private static void ParseEnvContent(string content, Dictionary<string, string> vars)
    {
        foreach (var line in content.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                continue;

            var equalsIndex = trimmed.IndexOf('=', StringComparison.Ordinal);
            if (equalsIndex <= 0)
                continue;

            var key = trimmed[..equalsIndex].Trim();
            var value = trimmed[(equalsIndex + 1)..].Trim();

            if (key.Length > 0)
                vars[key] = value;
        }
    }

    private async Task<string> ExtractAttachment(string project, string attachmentName)
    {
        var arguments = $"attachment-export --stdout \"{dbPath}\" \"{projectsGroup}/{project}\" \"{attachmentName}\"";
        var result = await processRunner.Run("keepassxc-cli", arguments, 30_000, stdinInput: $"{password}\n").ConfigureAwait(false);

        if (result.ExitCode != 0)
        {
            logger.LogKeePassCliFailed(result.ExitCode, projectsGroup, project, attachmentName, result.Stderr);
            return string.Empty;
        }

        return result.Stdout;
    }
}