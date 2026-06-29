using Moq;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Tests;

public class BaseTestClass : WebApplicationFactory<Program>
{
    public BaseTestClass()
    {
        Environment.SetEnvironmentVariable("GhcrUser", "test-user");
        Environment.SetEnvironmentVariable("ImageRepo", "ghcr.io/michaeltg17/deployer");
        Environment.SetEnvironmentVariable("DeployBaseDir", "/tmp/test-deploy");
        Environment.SetEnvironmentVariable("GhcrToken", "test-token");
        Environment.SetEnvironmentVariable("KeePassDbPath", "/tmp/test.kdbx");
        Environment.SetEnvironmentVariable("KeePassDbPassword", "test-db-pass");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(MockDockerClient);
    }

    static void MockDockerClient(IServiceCollection services)
    {
        var mock = new Mock<IDockerClient>();

        var imagesMock = new Mock<IImageOperations>();
        imagesMock.Setup(x => x.CreateImageAsync(
            It.Is<ImagesCreateParameters>(p => p.FromImage == "ghcr.io/michaeltg17/deployer"),
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

        mock.Setup(x => x.Images).Returns(imagesMock.Object);

        var existing = services.FirstOrDefault(d => d.ServiceType == typeof(IDockerClient));
        if (existing != null)
            services.Remove(existing);
        services.AddSingleton(mock.Object);
    }
}
