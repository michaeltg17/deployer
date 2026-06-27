using System.Diagnostics;
using Api.Exceptions;
using Api.Models;
using Api.Validation;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Options;

namespace Api.Services;

public sealed class DeploymentService(
    ILogger<DeploymentService> logger,
    IOptions<DeployerSettings> settings,
    IDockerClient dockerClient, KeePassEnvService keepassEnvService)
{
    private readonly DeployerSettings deployerSettings = settings.Value;

    public async Task Deploy(DeployRequest request)
    {
        if (DeployRequestValidator.Validate(request) is { } validationEx)
            throw validationEx;

        var projectDir = Path.Combine("/projects", request.Project!);
        var composeFile = Path.Combine(projectDir, "docker-compose.yml");

        if (!File.Exists(composeFile))
            throw new InvalidDeployRequestException($"Docker compose file not found for project '{request.Project}': {composeFile}");

        var image = $"{deployerSettings.ImageRepo}:{request.Tag}";
        var tempDir = Path.Combine(Path.GetTempPath(), $"deploy-{request.Project}-{request.Environment}-{Guid.NewGuid():N}");

        try
        {
            logger.LogInformation("Deploying {Image} to {Project}/{Environment}", image, request.Project, request.Environment);

            Directory.CreateDirectory(tempDir);
            File.Copy(composeFile, Path.Combine(tempDir, "docker-compose.yml"));

            logger.LogInformation("Extracting .env for {Project}/{Environment} from KeePass", request.Project, request.Environment);
            await keepassEnvService.WriteEnvFiles(tempDir, request.Project!, request.Environment!);

            logger.LogInformation("Pulling image: {Image}", image);
            var authConfig = new AuthConfig
            {
                Username = deployerSettings.GhcrUser,
                Password = deployerSettings.GhcrToken,
                ServerAddress = "ghcr.io"
            };
            await dockerClient.Images.CreateImageAsync(
                new ImagesCreateParameters { FromImage = deployerSettings.ImageRepo, Tag = request.Tag },
                authConfig,
                new Progress<JSONMessage>());
            logger.LogInformation("Image pulled successfully");

            var tempComposeFile = Path.Combine(tempDir, "docker-compose.yml");
            logger.LogInformation("Running docker compose in {TempDir}", tempDir);
            var composeResult = await RunComposeUp(tempComposeFile, request.Tag!);
            if (composeResult.ExitCode != 0)
            {
                logger.LogError("Compose up failed: {Stderr}", composeResult.Stderr);
                throw new DeployerException($"Failed to start services: {composeResult.Stderr}");
            }

            logger.LogInformation("Successfully deployed tag {Tag} to {Project}/{Environment}", request.Tag, request.Project, request.Environment);
        }
        finally
        {
            await keepassEnvService.Cleanup(tempDir);
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to remove temp dir {TempDir}", tempDir);
            }
        }
    }

    private async Task<(int ExitCode, string Stdout, string Stderr)> RunComposeUp(string composeFile, string tag)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"compose -f \"{composeFile}\" up -d --force-recreate",
            WorkingDirectory = Path.GetDirectoryName(composeFile),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            EnvironmentVariables = { ["TAG"] = tag }
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start process: docker compose -f {composeFile} up -d --force-recreate");

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(300_000))
        {
            process.Kill();
            throw new TimeoutException("Docker compose up timed out");
        }

        return (process.ExitCode, await stdoutTask, await stderrTask);
    }
}