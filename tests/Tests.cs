using System.Net;
using System.Text;
using Api.Models;
using Xunit;

namespace Tests;

public sealed class DeployTests : IClassFixture<BaseTestClass>
{
    private readonly BaseTestClass factory;
    private readonly HttpClient client;

    public DeployTests(BaseTestClass factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        this.factory = factory;
        client = factory.CreateClient();
    }

    [Fact]
    public async Task MissingBody_Returns400()
    {
        var response = await client.PostAsync(new Uri("/", UriKind.Relative), null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task InvalidBody_Returns400()
    {
        using var content = new StringContent("not-json", Encoding.UTF8, "application/json");
        var response = await client.PostAsync(new Uri("/", UriKind.Relative), content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MissingEnvironment_Returns400()
    {
        var body = new DeployRequest { Project = "test", Tag = "v1.0.0" };
        using var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(new Uri("/", UriKind.Relative), content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MissingTag_Returns400()
    {
        var body = new DeployRequest { Project = "test", Environment = "dev" };
        using var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(new Uri("/", UriKind.Relative), content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ValidRequest_NoComposeFile_Returns400()
    {
        var body = new DeployRequest
        {
            Project = "test",
            Environment = "dev",
            Tag = "v1.0.0"
        };
        using var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(new Uri("/", UriKind.Relative), content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errorResponse = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("docker-compose.yml", errorResponse, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ValidRequest_EachEnvironment_Returns400()
    {
        foreach (var environment in new[] { "dev", "qa", "prod" })
        {
            var body = new DeployRequest
            {
                Project = "test",
                Environment = environment,
                Tag = "v1.0.0"
            };
            using var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(new Uri("/", UriKind.Relative), content, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }

    [Fact]
    public async Task ValidRequest_Returns200()
    {
        var projectName = "test-ok";
        var projectDir = Path.Combine(factory.TestProjectsDir, projectName);
        Directory.CreateDirectory(projectDir);
        await File.WriteAllTextAsync(Path.Combine(projectDir, "docker-compose.yml"), "services:\n  app:\n    image: test:test\n", TestContext.Current.CancellationToken);
        try
        {
            var body = new DeployRequest
            {
                Project = projectName,
                Environment = "dev",
                Tag = "v1.0.0"
            };
            using var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(new Uri("/", UriKind.Relative), content, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        finally
        {
            Directory.Delete(projectDir, true);
        }
    }
}