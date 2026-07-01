namespace Api.Models;

internal sealed class ProcessResult
{
    public int ExitCode { get; init; }
    public required string Stdout { get; init; }
    public required string Stderr { get; init; }
}
