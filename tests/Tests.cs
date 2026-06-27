using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Api.Models;
using Xunit;

namespace Tests;

public class Tests : IClassFixture<BaseTestClass>
{
    private readonly HttpClient client;

    public Tests(BaseTestClass factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task MissingBody_Returns400()
    {
        var response = await client.PostAsync("/deploy", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task InvalidBody_Returns400()
    {
        var response = await client.PostAsync("/deploy",
            new StringContent("not-json", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MissingEnvironment_Returns400()
    {
        var body = new DeployRequest { Tag = "v1.0.0" };
        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/deploy", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MissingTag_Returns400()
    {
        var body = new DeployRequest { Environment = "dev" };
        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/deploy", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task InvalidEnvironment_Returns400()
    {
        var body = new DeployRequest { Environment = "staging", Tag = "v1.0.0" };
        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/deploy", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task NoAuth_Returns401()
    {
        var unauthenticatedClient = client;
        unauthenticatedClient.DefaultRequestHeaders.Authorization = null;

        var body = new DeployRequest { Environment = "dev", Tag = "v1.0.0" };
        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await unauthenticatedClient.PostAsync("/deploy", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task WrongCredentials_Returns401()
    {
        var badClient = client;
        badClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes("deploy:wrong")));

        var body = new DeployRequest { Environment = "dev", Tag = "v1.0.0" };
        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await badClient.PostAsync("/deploy", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ValidRequest_NoComposeFile_Returns500()
    {
        var body = new DeployRequest
        {
            Environment = "dev",
            Tag = "v1.0.0"
        };
        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/deploy", content);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var errorResponse = await response.Content.ReadAsStringAsync();
        Assert.Contains("docker-compose.yml", errorResponse);
    }

    [Fact]
    public async Task ValidRequest_EachEnvironment_Returns500()
    {
        foreach (var environment in new[] { "dev", "qa", "prod" })
        {
            var body = new DeployRequest
            {
                Environment = environment,
                Tag = "v1.0.0"
            };
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/deploy", content);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }
    }
}
