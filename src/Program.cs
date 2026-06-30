using Api.Endpoints;
using Api.Extensions;
using Api.Models;
using Api.Services;
using Api.Validation;
using Docker.DotNet;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<DeployerSettings>()
    .BindConfiguration("")
    .ValidateOnStart()
    .Services.AddSingleton<IValidateOptions<DeployerSettings>, DeployerSettingsValidator>();

builder.Services.AddProblemDetails();
builder.Services.AddSingleton<IProcessRunner, ProcessRunner>();
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

app.UseCustomExceptionHandler();
DeployEndpoint.Map(app);

app.Run();