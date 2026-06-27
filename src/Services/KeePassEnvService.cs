using System.Diagnostics;
using System.Text;
using Api.Logging;
using Api.Models;
using Microsoft.Extensions.Options;

namespace Api.Services;

public sealed class KeePassEnvService(ILogger<KeePassEnvService> logger, IOptions<DeployerSettings> settings)
{
    private readonly string dbPath = settings.Value.KeePassDbPath!;
    private readonly string password = settings.Value.KeePassDbPassword!;
    private readonly string projectsGroup = "Projects";

    public async Task WriteEnvFiles(string targetDir, string project, string environment)
    {
        var sb = new StringBuilder();

        var common = await ExtractAttachment(project, ".env");
        if (!string.IsNullOrEmpty(common))
        {
            sb.Append(common);
            if (!common.EndsWith('\n'))
                sb.AppendLine();
        }

        var envSpecific = await ExtractAttachment(project, $".env.{environment}");
        if (!string.IsNullOrEmpty(envSpecific))
            sb.Append(envSpecific);

        var envPath = Path.Combine(targetDir, ".env");
        await File.WriteAllTextAsync(envPath, sb.ToString());
        logger.LogEnvWritten(targetDir, project, environment);
    }

    public async Task Cleanup(string targetDir)
    {
        try
        {
            var envPath = Path.Combine(targetDir, ".env");
            if (File.Exists(envPath))
                File.Delete(envPath);
        }
        catch (Exception ex)
        {
            logger.LogEnvCleanupFailed(targetDir, ex);
        }
    }

    private async Task<string> ExtractAttachment(string project, string attachmentName)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "keepassxc-cli",
            Arguments = $"--password \"{password}\" attachment-export --stdout \"{dbPath}\" \"{projectsGroup}/{project}\" \"{attachmentName}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start keepassxc-cli");

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        process.WaitForExit(30_000);

        if (process.ExitCode != 0)
        {
            var stderr = await stderrTask;
            logger.LogKeePassCliFailed(process.ExitCode, projectsGroup, project, attachmentName, stderr);
            return string.Empty;
        }

        return await stdoutTask;
    }
}