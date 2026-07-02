using Api.Models;
using Api.Services;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Tests;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Tests public")]
public sealed class RealDockerTestClass : WebApplicationFactory<Program>, IAsyncDisposable
{
    public string TestProjectsDir { get; }
    private readonly string testKdbxPath;
    private readonly DockerClient dockerClient;

    public IDockerClient DockerClient => Services.GetRequiredService<IDockerClient>();

    public RealDockerTestClass()
    {
        var testRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(typeof(RealDockerTestClass).Assembly.Location)!, "..", "..", ".."));
        testKdbxPath = Path.Combine(testRoot, "test.kdbx");
        TestProjectsDir = Path.Combine(testRoot, "projects");

        var config = new DockerClientConfiguration();
        dockerClient = config.CreateClient();
        config.Dispose();
        CleanupTestContainers().Wait();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { nameof(DeployerSettings.ImageRepo), "ghcr.io/michaeltg17/deployer" },
                { nameof(DeployerSettings.KeePassDbPath), testKdbxPath },
                { nameof(DeployerSettings.KeePassDbPassword), "test-db-pass" },
                { nameof(DeployerSettings.ProjectsDir), TestProjectsDir },
            });
        });

        builder.ConfigureServices(services =>
        {
            var existing = services.FirstOrDefault(d => d.ServiceType == typeof(IProcessRunner));
            if (existing != null)
                services.Remove(existing);
            services.AddSingleton<IProcessRunner>(sp => new DelegatingProcessRunner(new ProcessRunner()));
        });
    }

    private async Task CleanupTestContainers()
    {
        try
        {
            var containers = await dockerClient.Containers.ListContainersAsync(
                new ContainersListParameters { All = true }).ConfigureAwait(false);
            var testContainers = containers
                .Where(c => c.Names.Any(n => n.StartsWith("/deployer-test-", StringComparison.Ordinal)))
                .ToList();

            foreach (var container in testContainers)
            {
                if (container.State == "running")
                {
                    await dockerClient.Containers.StopContainerAsync(container.ID,
                        new ContainerStopParameters()).ConfigureAwait(false);
                }
                await dockerClient.Containers.RemoveContainerAsync(container.ID,
                    new ContainerRemoveParameters { Force = true }).ConfigureAwait(false);
            }
        }
        catch
        {
            // Ignore cleanup errors, containers may not exist
        }
    }

    public async override ValueTask DisposeAsync()
    {
        await CleanupTestContainers().ConfigureAwait(false);
        dockerClient.Dispose();
        await base.DisposeAsync().ConfigureAwait(false);
    }
}