using System.Text;
using Api.Logging;
using Api.Models;
using Microsoft.Extensions.Options;

namespace Api.Services;

internal sealed class KeePassEnvService(
    ILogger<KeePassEnvService> logger,
    IOptions<DeployerSettings> settings,
    IProcessRunner processRunner)
{
    private readonly string dbPath = settings.Value.KeePassDbPath!;
    private readonly string password = settings.Value.KeePassDbPassword!;
    private readonly string projectsGroup = "Projects";

    public async Task WriteEnvFiles(string targetDir, string project, string environment)
    {
        var sb = new StringBuilder();

        var common = await ExtractAttachment(project, ".env").ConfigureAwait(false);
        if (!string.IsNullOrEmpty(common))
        {
            sb.Append(common);
            if (!common.EndsWith('\n'))
                sb.AppendLine();
        }

        var envSpecific = await ExtractAttachment(project, $".env.{environment}").ConfigureAwait(false);
        if (!string.IsNullOrEmpty(envSpecific))
            sb.Append(envSpecific);

        var envPath = Path.Combine(targetDir, ".env");
        await File.WriteAllTextAsync(envPath, sb.ToString()).ConfigureAwait(false);
        logger.LogEnvWritten(targetDir, project, environment);
    }

    public static async Task Cleanup(string targetDir)
    {
        var envPath = Path.Combine(targetDir, ".env");
        if (File.Exists(envPath))
            File.Delete(envPath);
    }

    private async Task<string> ExtractAttachment(string project, string attachmentName)
    {
        var arguments = $"--password \"{password}\" attachment-export --stdout \"{dbPath}\" \"{projectsGroup}/{project}\" \"{attachmentName}\"";
        var result = await processRunner.Run("keepassxc-cli", arguments, 30_000).ConfigureAwait(false);

        if (result.ExitCode != 0)
        {
            logger.LogKeePassCliFailed(result.ExitCode, projectsGroup, project, attachmentName, result.Stderr);
            return string.Empty;
        }

        return result.Stdout;
    }
}