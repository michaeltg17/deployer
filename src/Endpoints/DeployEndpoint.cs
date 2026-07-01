using Microsoft.AspNetCore.Mvc;
using Api.Models;
using Api.Services;

namespace Api.Endpoints;

internal static class DeployEndpoint
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/", async (
            [FromBody] DeployRequest request,
            [FromServices] DeploymentService deployService) =>
        {
            await deployService.Deploy(request).ConfigureAwait(false);
            return Results.Ok();
        });
    }
}