using Microsoft.AspNetCore.Mvc;
using Api.Models;
using Api.Services;

namespace Api.Endpoints;

public static class DeployEndpoint
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/deploy", async (
            [FromBody] DeployRequest request,
            [FromServices] DeploymentService deployService,
            ILogger<DeploymentService> logger) =>
        {
            if (string.IsNullOrEmpty(request.Project) || string.IsNullOrEmpty(request.Environment) || string.IsNullOrEmpty(request.Tag))
                return Results.Problem($"Missing fields. Got project={request.Project}, environment={request.Environment}, tag={request.Tag}", statusCode: 400);

            var (success, message) = await deployService.Deploy(request.Project, request.Environment, request.Tag);

            if (!success)
                logger.LogError("Deployment failed: {Message}", message);

            return success ? Results.Ok() : Results.Problem(message, statusCode: 500);
        });
    }
}