using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Api.Models;
using Api.Services;

namespace Api.Endpoints;

public static class DeployEndpoint
{
    static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static void MapDeployEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/deploy", async (
            HttpRequest httpRequest,
            [FromServices] DeploymentService deployService,
            [FromServices] IOptions<DeployerSettings> settingsOption,
            ILogger<DeploymentService> logger) =>
        {
            var settings = settingsOption.Value;

            byte[] payload;
            await using (var buffer = new MemoryStream())
            {
                await httpRequest.Body.CopyToAsync(buffer);
                payload = buffer.ToArray();
            }

            if (payload.Length == 0)
                return Results.Problem("Request body required", statusCode: 400);

            DeployRequest? body;
            body = ParseBody(Encoding.UTF8.GetString(payload));
            if (body is null)
                return Results.Problem("Invalid JSON", statusCode: 400);

            var project = body.Project ?? httpRequest.Query["project"];
            var env = body.Environment ?? httpRequest.Query["environment"];
            var tg = body.Tag ?? httpRequest.Query["tag"];

            if (string.IsNullOrEmpty(project) || string.IsNullOrEmpty(env) || string.IsNullOrEmpty(tg))
                return Results.Problem($"Missing fields. Got project={project}, environment={env}, tag={tg}", statusCode: 400);

            var (success, message) = await deployService.DeployAsync(project!, env!, tg!);

            if (!success)
                logger.LogError("Deployment failed: {Message}", message);

            return success ? Results.Ok() : Results.Problem(message, statusCode: 500);
        });
    }

    static DeployRequest? ParseBody(string rawText)
    {
        try
        {
            return JsonSerializer.Deserialize<DeployRequest>(rawText, jsonOptions);
        }
        catch (JsonException)
        {
            try
            {
                return JsonSerializer.Deserialize<DeployRequest>(SanitizeJson(rawText), jsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }

    static string SanitizeJson(string text)
    {
        var result = new StringBuilder();
        var inString = false;
        var escaped = false;
        foreach (var @char in text)
        {
            if (escaped)
            {
                result.Append(@char);
                escaped = false;
                continue;
            }
            if (@char == '\\')
            {
                escaped = true;
                result.Append(@char);
                continue;
            }
            if (@char == '"')
            {
                inString = !inString;
                result.Append(@char);
                continue;
            }
            if (inString && (int)@char < 0x20)
            {
                string replacement = @char switch
                {
                    '\n' => "\\n",
                    '\r' => "\\r",
                    '\t' => "\\t",
                    '\b' => "\\b",
                    '\f' => "\\f",
                    _ => $"\\u{(int)@char:X4}"
                };
                result.Append(replacement);
            }
            else
            {
                result.Append(@char);
            }
        }
        return result.ToString();
    }
}