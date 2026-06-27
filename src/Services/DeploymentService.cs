using System.Diagnostics;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Options;
using Api.Models;

namespace Api.Services;

public sealed class DeploymentService(ILogger<DeploymentService> logger, IOptions<DeployerSettings> settings, IDockerClient dockerClient)
{
    private readonly DeployerSettings _settings = settings.Value;

    public async Task<(bool Success, string Message)> DeployAsync(string environment, string tag)
    {
        var composeFile = Path.Combine(_settings.DeployBaseDir, "docker-compose.yml");
        if (!File.Exists(composeFile))
            return (false, $"Docker compose file not found: {composeFile}");

        var image = $"{_settings.ImageRepo}:{tag}";
        var serviceName = $"{environment}";

        logger.LogInformation("Deploying {Image} to {Environment}", image, environment);

        var authConfig = new AuthConfig
        {
            Username = _settings.GhcrUser,
            Password = _settings.GhcrToken,
            ServerAddress = "ghcr.io"
        };

        try
        {
            logger.LogInformation("Pulling image: {Image}", image);
            await dockerClient.Images.CreateImageAsync(
                new ImagesCreateParameters { FromImage = _settings.ImageRepo, Tag = tag },
                authConfig,
                new Progress<JSONMessage>());
            logger.LogInformation("Image pulled successfully");

            logger.LogInformation("Deploying {Service}...", serviceName);
            var composeResult = await RunComposeUpAsync(composeFile, serviceName, tag);
            if (composeResult.ExitCode != 0)
            {
                logger.LogError("Service start failed: {Stderr}", composeResult.Stderr);
                return (false, $"Failed to start service: {composeResult.Stderr}");
            }
            logger.LogInformation("Service {Service} deployed successfully", serviceName);

            logger.LogInformation("Cleaning up old images...");
            var pruned = await dockerClient.Images.PruneImagesAsync(new ImagesPruneParameters());
            logger.LogInformation("Pruned {Count} unused images, reclaimed {Space} bytes",
                pruned.ImagesDeleted?.Count ?? 0, pruned.SpaceReclaimed);

            logger.LogInformation("Successfully deployed tag {Tag} to {Environment}", tag, environment);
            return (true, $"Successfully deployed tag {tag} to {environment}");
        }
        catch (Exception ex)
        {
            return (false, $"Deployment failed: {ex.Message}");
        }
    }

    private async Task<(int ExitCode, string Stdout, string Stderr)> RunComposeUpAsync(string composeFile, string serviceName, string tag)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"compose -f {composeFile} up -d --force-recreate {serviceName}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            EnvironmentVariables = { ["TAG"] = tag }
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start process: docker compose -f {composeFile} up -d --force-recreate {serviceName}");

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
