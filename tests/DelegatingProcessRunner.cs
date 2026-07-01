using Api.Models;
using Api.Services;

namespace Tests;

sealed class DelegatingProcessRunner(IProcessRunner inner) : IProcessRunner
{
    public async Task<ProcessResult> Run(
        string fileName,
        string arguments,
        int timeoutMs,
        string? workingDirectory = null,
        Dictionary<string, string>? environmentVariables = null,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(fileName, "keepassxc-cli", StringComparison.OrdinalIgnoreCase))
            return new ProcessResult { ExitCode = 1, Stdout = "", Stderr = "keepassxc-cli not found" };

        return await inner.Run(fileName, arguments, timeoutMs, workingDirectory, environmentVariables, cancellationToken).ConfigureAwait(false);
    }
}
