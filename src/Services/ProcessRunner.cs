using Api.Models;
using System.Diagnostics;

namespace Api.Services;

public sealed class ProcessRunner : IProcessRunner
{
    public async Task<ProcessResult> Run(
        string fileName,
        string arguments,
        int timeoutMs,
        string? workingDirectory = null,
        Dictionary<string, string>? environmentVariables = null,
        CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        if (workingDirectory != null)
            psi.WorkingDirectory = workingDirectory;

        if (environmentVariables != null)
        {
            foreach (var (key, value) in environmentVariables)
                psi.EnvironmentVariables[key] = value;
        }

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start process: {fileName} {arguments}");

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(timeoutMs))
        {
            process.Kill();
            throw new TimeoutException($"Process timed out: {fileName} {arguments}");
        }

        return new ProcessResult
        {
            ExitCode = process.ExitCode,
            Stdout = await stdoutTask,
            Stderr = await stderrTask
        };
    }
}
