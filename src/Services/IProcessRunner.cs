using Api.Models;

namespace Api.Services;

internal interface IProcessRunner
{
    Task<ProcessResult> Run(
        string fileName,
        string arguments,
        int timeoutMs,
        string? workingDirectory = null,
        Dictionary<string, string>? environmentVariables = null,
        CancellationToken cancellationToken = default);
}
