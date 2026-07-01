using Api.Models;
using Api.Services;
using Docker.DotNet;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Tests;

public class RealDockerTestClass : WebApplicationFactory<Program>, IDisposable
{
    public string TestProjectsDir { get; }
    private readonly string testKdbxPath;

    private readonly IDockerClient dockerClient;

    public RealDockerTestClass()
    {
        var testRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(typeof(RealDockerTestClass).Assembly.Location)!, "..", "..", ".."));
        testKdbxPath = Path.Combine(testRoot, "test.kdbx");
        TestProjectsDir = Path.Combine(testRoot, "projects");

        dockerClient = new DockerClientConfiguration().CreateClient();
        CleanupTestContainers();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
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

    public IDockerClient DockerClient => Services.GetRequiredService<IDockerClient>();

    private void CleanupTestContainers()
    {
        try
        {
            var containers = dockerClient.Containers.ListContainersAsync(
                new Docker.DotNet.Models.ContainersListParameters { All = true }).Result;
            var testContainers = containers.Where(c => c.Names.Any(n => n.StartsWith("/deployer-test-"))).ToList();

            foreach (var container in testContainers)
            {
                if (container.State == "running")
                {
                    dockerClient.Containers.StopContainerAsync(container.ID,
                        new Docker.DotNet.Models.ContainerStopParameters()).Wait();
                }
                dockerClient.Containers.RemoveContainerAsync(container.ID,
                    new Docker.DotNet.Models.ContainerRemoveParameters { Force = true }).Wait();
            }
        }
        catch
        {
            // Ignore cleanup errors, containers may not exist
        }
    }

    public new void Dispose()
    {
        CleanupTestContainers();
        dockerClient.Dispose();
        base.Dispose();
    }
}