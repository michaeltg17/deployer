using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Tests;

public class BaseTestClass : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var testSettings = new Dictionary<string, string?>
            {
                ["GhcrUser"] = "test-user",
                ["ImageRepo"] = "ghcr.io/michaeltg17/deployer",
                ["DeployBaseDir"] = "/tmp/test-deploy",
                ["GhcrToken"] = "test-token",
                ["KeePassDbPath"] = "/tmp/test.kdbx",
                ["KeePassDbPassword"] = "test-db-pass"
            };

            config.AddInMemoryCollection(testSettings);
        });

        builder.ConfigureServices(services =>
        {
            MockDockerClient(services);
        });
    }

    private static void MockDockerClient(IServiceCollection services)
    {
        var mock = new Mock<IDockerClient>();

        var imagesMock = new Mock<IImageOperations>();

        imagesMock.Setup(x => x.CreateImageAsync(
            It.Is<ImagesCreateParameters>(p =>
                p.FromImage == "ghcr.io/michaeltg17/deployer"),
            It.IsAny<AuthConfig>(),
            It.IsAny<IProgress<JSONMessage>>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        imagesMock.Setup(x => x.PruneImagesAsync(
            It.IsAny<ImagesPruneParameters>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImagesPruneResponse
            {
                ImagesDeleted = Array.Empty<ImageDeleteResponse>(),
                SpaceReclaimed = 0
            });

        mock.Setup(x => x.Images)
            .Returns(imagesMock.Object);

        var existing = services.FirstOrDefault(
            d => d.ServiceType == typeof(IDockerClient));

        if (existing != null)
        {
            services.Remove(existing);
        }

        services.AddSingleton(mock.Object);
    }
}