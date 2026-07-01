using System.Net;
using System.Text;
using Api.Models;
using Docker.DotNet;
using Xunit;

namespace Tests;

public class RealDockerTests(RealDockerTestClass factory) : IClassFixture<RealDockerTestClass>
{
    private readonly HttpClient client = factory.CreateClient();
    private readonly IDockerClient dockerClient = factory.DockerClient;

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
            await StopAndRemoveContainer(containerName);
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
        var response = await client.PostAsync("/", content);

        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"{response.StatusCode}: {responseBody}");

        var containers = await dockerClient.Containers.ListContainersAsync(
            new Docker.DotNet.Models.ContainersListParameters { All = true });
        var container = containers.FirstOrDefault(c => c.Names.Any(n => n == $"/{containerName}"));
        Assert.NotNull(container);
        Assert.Equal(expectedImage, container.Image);
    }

    async Task StopAndRemoveContainer(string name)
    {
        var containers = await dockerClient.Containers.ListContainersAsync(
            new Docker.DotNet.Models.ContainersListParameters { All = true });
        var container = containers.FirstOrDefault(c => c.Names.Any(n => n == $"/{name}"));
        if (container == null)
            return;

        await dockerClient.Containers.StopContainerAsync(container.ID, new Docker.DotNet.Models.ContainerStopParameters());
        await dockerClient.Containers.RemoveContainerAsync(container.ID, new Docker.DotNet.Models.ContainerRemoveParameters { Force = true });
    }
}