using Api.Models;
using Api.Services;
using Docker.DotNet;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Tests;

public class RealDockerTestClass : WebApplicationFactory<Program>
{
    public string TestProjectsDir { get; }

    public RealDockerTestClass()
    {
        Environment.SetEnvironmentVariable(nameof(DeployerSettings.ImageRepo), "nginx");
        Environment.SetEnvironmentVariable(nameof(DeployerSettings.KeePassDbPath), "/tmp/test.kdbx");
        Environment.SetEnvironmentVariable(nameof(DeployerSettings.KeePassDbPassword), "test-db-pass");

        TestProjectsDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"test-projects-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(TestProjectsDir);
        Environment.SetEnvironmentVariable(nameof(DeployerSettings.ProjectsDir), TestProjectsDir);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            var existing = services.FirstOrDefault(d => d.ServiceType == typeof(IProcessRunner));
            if (existing != null)
                services.Remove(existing);
            services.AddSingleton<IProcessRunner>(sp => new DelegatingProcessRunner(new ProcessRunner()));
        });
    }
}
