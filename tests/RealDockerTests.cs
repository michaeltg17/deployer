using System.Diagnostics;
using System.Net;
using System.Text;
using Api.Models;
using Xunit;

namespace Tests;

public class RealDockerTests(RealDockerTestClass factory) : IClassFixture<RealDockerTestClass>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task ValidRequest_Latest_Returns200_AndStartsContainer()
    {
        await DeployAndVerify("test-project", "dev", "latest", "ghcr.io/michaeltg17/deployer:latest");
    }

    [Fact]
    public async Task ValidRequest_CommitTag_Returns200_AndStartsContainer()
    {
        await DeployAndVerify("test-project", "dev", "21ec91a", "ghcr.io/michaeltg17/deployer:21ec91a");
    }

    async Task DeployAndVerify(string project, string environment, string tag, string expectedImage)
    {
        var containerName = $"deployer-test-{tag}";

        try
        {
            _ = await RunProcess("docker", $"stop {containerName}");
            _ = await RunProcess("docker", $"rm {containerName}");
        }
        catch { }

        var body = new DeployRequest
        {
            Project = project,
            Environment = environment,
            Tag = tag,
        };
        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/", content);

        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"{response.StatusCode}: {responseBody}");

        var inspectResult = await RunProcess("docker", $"inspect --format '{{{{.Config.Image}}}}' {containerName}");
        Assert.Equal(0, inspectResult.ExitCode);
        Assert.Equal(expectedImage, inspectResult.Stdout.Trim().Trim('"', '\''));
    }

    static async Task<ProcessResult> RunProcess(string fileName, string arguments)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        }) ?? throw new InvalidOperationException($"Failed to start process: {fileName} {arguments}");

        process.WaitForExit(60_000);
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();

        return new ProcessResult
        {
            ExitCode = process.ExitCode,
            Stdout = stdout,
            Stderr = stderr
        };
    }
}
