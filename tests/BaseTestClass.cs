using Api.Models;
using Api.Services;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Tests;

public class BaseTestClass : WebApplicationFactory<Program>
{
    public string TestProjectsDir { get; }

    public BaseTestClass()
    {
        TestProjectsDir = Path.Combine(Path.GetTempPath(), $"test-projects-{Guid.NewGuid():N}");
        Directory.CreateDirectory(TestProjectsDir);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { nameof(DeployerSettings.ImageRepo), "ghcr.io/michaeltg17/deployer" },
                { nameof(DeployerSettings.KeePassDbPath), "/tmp/test.kdbx" },
                { nameof(DeployerSettings.KeePassDbPassword), "test-db-pass" },
                { nameof(DeployerSettings.ProjectsDir), TestProjectsDir },
            });
        });

        builder.ConfigureServices(services =>
        {
            MockDockerClient(services);
            MockProcessRunner(services);
        });
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

    static void MockProcessRunner(IServiceCollection services)
    {
        var mock = new Mock<IProcessRunner>();

        mock.Setup(x => x.Run(
            "keepassxc-cli",
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<string?>(),
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult { ExitCode = 0, Stdout = "ENV_VAR=value\n", Stderr = "" });

        mock.Setup(x => x.Run(
            "docker",
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<string?>(),
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult { ExitCode = 0, Stdout = "", Stderr = "" });

        var existing = services.FirstOrDefault(d => d.ServiceType == typeof(IProcessRunner));
        if (existing != null)
            services.Remove(existing);
        services.AddSingleton(mock.Object);
    }
}
