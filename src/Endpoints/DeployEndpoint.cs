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
            [FromServices] DeploymentService deployService) =>
        {
            await deployService.Deploy(request);
            return Results.Ok();
        });
    }
}