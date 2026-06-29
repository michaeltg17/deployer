using System.Net;
using System.Text;
using Api.Models;
using Xunit;

namespace Tests;

public class Tests(BaseTestClass factory) : IClassFixture<BaseTestClass>
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task MissingBody_Returns400()
    {
        var response = await client.PostAsync("/", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task InvalidBody_Returns400()
    {
        var response = await client.PostAsync("/",
            new StringContent("not-json", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MissingEnvironment_Returns400()
    {
        var body = new DeployRequest { Project = "test", Tag = "v1.0.0" };
        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MissingTag_Returns400()
    {
        var body = new DeployRequest { Project = "test", Environment = "dev" };
        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/", content);

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
        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errorResponse = await response.Content.ReadAsStringAsync();
        Assert.Contains("docker-compose.yml", errorResponse);
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
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/", content);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
