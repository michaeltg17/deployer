using Api.Endpoints;
using Api.Models;
using Api.Services;
using Api.Validation;
using Docker.DotNet;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<DeployerSettings>()
    .BindConfiguration("")
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<DeployerSettings>, DeployerSettingsValidation>();

builder.Services.AddSingleton<IDockerClient>(sp =>
{
    var config = new DockerClientConfiguration();
    return config.CreateClient();
});
builder.Services.AddSingleton<KeePassEnvService>();
builder.Services.AddSingleton<DeploymentService>();

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole();

var app = builder.Build();

DeployEndpoint.MapDeployEndpoint(app);

app.Run();