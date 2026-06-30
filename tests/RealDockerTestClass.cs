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
        var testRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(typeof(RealDockerTestClass).Assembly.Location)!, "..", "..", ".."));
        var testKdbxPath = Path.Combine(testRoot, "test.kdbx");
        TestProjectsDir = Path.Combine(testRoot, "projects");

        Environment.SetEnvironmentVariable(nameof(DeployerSettings.ImageRepo), "ghcr.io/michaeltg17/deployer");
        Environment.SetEnvironmentVariable(nameof(DeployerSettings.KeePassDbPath), testKdbxPath);
        Environment.SetEnvironmentVariable(nameof(DeployerSettings.KeePassDbPassword), "test-db-pass");
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

            // Override DeployerSettings via post-configuration
            services.PostConfigure<DeployerSettings>(opts =>
            {
                opts.ProjectsDir = TestProjectsDir;
            });
        });
    }
}
