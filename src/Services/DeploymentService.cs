using Api.Exceptions;
using Api.Logging;
using Api.Models;
using Api.Validation;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Options;

namespace Api.Services;

public sealed class DeploymentService(
    ILogger<DeploymentService> logger,
    IOptions<DeployerSettings> settings,
    IDockerClient dockerClient, KeePassEnvService keepassEnvService, IProcessRunner processRunner)
{
    private readonly DeployerSettings deployerSettings = settings.Value;

    public async Task Deploy(DeployRequest request)
    {
        if (DeployRequestValidator.Validate(request) is { } validationEx)
            throw validationEx;

        var projectDir = Path.Combine(deployerSettings.ProjectsDir, request.Project!);
        var composeFile = Path.Combine(projectDir, "docker-compose.yml");

        if (!File.Exists(composeFile))
            throw new InvalidDeployRequestException($"Docker compose file not found for project '{request.Project}': {composeFile}");

        var image = $"{deployerSettings.ImageRepo}:{request.Tag}";
        var tempDir = Path.Combine(Path.GetTempPath(), $"deploy-{request.Project}-{request.Environment}-{Guid.NewGuid():N}");

        try
        {
            logger.LogDeploying(image, request.Project!, request.Environment!);

            Directory.CreateDirectory(tempDir);
            File.Copy(composeFile, Path.Combine(tempDir, "docker-compose.yml"));

            logger.LogExtractingEnv(request.Project!, request.Environment!);
            await keepassEnvService.WriteEnvFiles(tempDir, request.Project!, request.Environment!);

            logger.LogPullingImage(image);
            await dockerClient.Images.CreateImageAsync(
                new ImagesCreateParameters { FromImage = deployerSettings.ImageRepo, Tag = request.Tag },
                null,
                new Progress<JSONMessage>());
            logger.LogImagePulled();

            var tempComposeFile = Path.Combine(tempDir, "docker-compose.yml");
            logger.LogRunningCompose(tempDir);
            var composeResult = await RunComposeUp(tempComposeFile, request.Tag!);
            if (composeResult.ExitCode != 0)
            {
                logger.LogComposeFailed(composeResult.Stderr);
                throw new DeployerException($"Failed to start services: {composeResult.Stderr}");
            }

            logger.LogDeploySuccess(request.Tag!, request.Project!, request.Environment!);
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
                logger.LogTempDirCleanupFailed(tempDir, ex);
            }
        }
    }

    private async Task<ProcessResult> RunComposeUp(string composeFile, string tag)
    {
        var arguments = $"compose -f \"{composeFile}\" up -d --force-recreate";
        var workingDir = Path.GetDirectoryName(composeFile) ?? ".";
        return await processRunner.Run("docker", arguments, 300_000, workingDir, new Dictionary<string, string> { ["TAG"] = tag });
    }
}