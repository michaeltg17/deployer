using System.Net;
using System.Text;
using Api.Models;
using Docker.DotNet;
using Docker.DotNet.Models;
using Xunit;

namespace Tests;

public sealed class RealDockerTests : IClassFixture<RealDockerTestClass>
{
    private readonly HttpClient client;
    private readonly IDockerClient dockerClient;

    public RealDockerTests(RealDockerTestClass factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        client = factory.CreateClient();
        dockerClient = factory.DockerClient;
    }

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
        await StopAndRemoveContainer(containerName).ConfigureAwait(false);

        var body = new DeployRequest
        {
            Project = project,
            Environment = environment,
            Tag = tag,
        };
        using var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(new Uri("/", UriKind.Relative), content).ConfigureAwait(false);

        var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"{response.StatusCode}: {responseBody}");

        var containers = await dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters { All = true }).ConfigureAwait(false);
        var container = containers.FirstOrDefault(c => c.Names.Any(n => n == $"/{containerName}"));
        Assert.NotNull(container);
        Assert.Equal(expectedImage, container.Image);

        var inspect = await dockerClient.Containers.InspectContainerAsync(container.ID).ConfigureAwait(false);
        Assert.NotNull(inspect.Config.Env);
        Assert.True(inspect.Config.Env!.Any(e => e == "COMMON=COMMON_VALUE"),
            $"COMMON=COMMON_VALUE not found in container environment. Env: {string.Join(", ", inspect.Config.Env ?? Array.Empty<string>())}");
        Assert.True(inspect.Config.Env!.Any(e => e == "SECRET=SECRET_DEV"),
            $"SECRET=SECRET_DEV not found in container environment. Env: {string.Join(", ", inspect.Config.Env ?? Array.Empty<string>())}");
        await StopAndRemoveContainer(containerName).ConfigureAwait(false);
    }

    async Task StopAndRemoveContainer(string name)
    {
        var containers = await dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters { All = true }).ConfigureAwait(false);
        var container = containers.FirstOrDefault(c => c.Names.Any(n => n == $"/{name}"));
        if (container == null)
            return;

        await dockerClient.Containers.StopContainerAsync(container.ID, new ContainerStopParameters())
            .ConfigureAwait(false);
        await dockerClient.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters { Force = true })
            .ConfigureAwait(false);
    }
}