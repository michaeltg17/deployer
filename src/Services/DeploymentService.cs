using Api.Exceptions;
using Api.Logging;
using Api.Models;
using Api.Validation;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Options;

namespace Api.Services;

internal sealed class DeploymentService(
    ILogger<DeploymentService> logger,
    IOptions<DeployerSettings> settings,
    IDockerClient dockerClient, KeePassEnvService keepassEnvService, IProcessRunner processRunner)
{
    private readonly DeployerSettings deployerSettings = settings.Value;

    public async Task Deploy(DeployRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (DeployRequestValidator.Validate(request) is { } validationEx)
            throw validationEx;

        var projectDir = Path.Combine(deployerSettings.ProjectsDir, request.Project!);
        var composeFile = Path.Combine(projectDir, "docker-compose.yml");

        if (!File.Exists(composeFile))
            throw new InvalidDeployRequestException($"Docker compose file not found for project '{request.Project}': {composeFile}");

        var image = $"{deployerSettings.ImageRepo}:{request.Tag}";

        logger.LogDeploying(image, request.Project!, request.Environment!);

        logger.LogExtractingEnv(request.Project!, request.Environment!);
        var envVars = await keepassEnvService.ExtractEnvVariables(request.Project!, request.Environment!).ConfigureAwait(false);
        envVars["TAG"] = request.Tag!;

        logger.LogPullingImage(image);
        await dockerClient.Images.CreateImageAsync(
            new ImagesCreateParameters { FromImage = deployerSettings.ImageRepo, Tag = request.Tag },
            null,
            new Progress<JSONMessage>()).ConfigureAwait(false);
        logger.LogImagePulled();

        logger.LogRunningCompose(composeFile);
        var composeResult = await RunComposeUp(composeFile, envVars).ConfigureAwait(false);
        if (composeResult.ExitCode != 0)
        {
            logger.LogComposeFailed(composeResult.Stderr);
            throw new DeployerException($"Failed to start services: {composeResult.Stderr}");
        }

        logger.LogDeploySuccess(request.Tag!, request.Project!, request.Environment!);
    }

    private async Task<ProcessResult> RunComposeUp(string composeFile, Dictionary<string, string> envVars)
    {
        var arguments = $"compose -f \"{composeFile}\" up -d --force-recreate";
        var workingDir = Path.GetDirectoryName(composeFile) ?? ".";
        return await processRunner.Run("docker", arguments, 300_000, workingDir, envVars).ConfigureAwait(false);
    }
}