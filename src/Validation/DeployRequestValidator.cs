using Api.Exceptions;
using Api.Models;

namespace Api.Validation;

internal static class DeployRequestValidator
{
    public static InvalidDeployRequestException? Validate(DeployRequest request)
    {
        if (request is null)
            return null;

        var invalidFields = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Project))
            invalidFields.Add($"project={ToJsonValue(request.Project)}");
        if (string.IsNullOrWhiteSpace(request.Environment))
            invalidFields.Add($"environment={ToJsonValue(request.Environment)}");
        if (string.IsNullOrWhiteSpace(request.Tag))
            invalidFields.Add($"tag={ToJsonValue(request.Tag)}");

        if (invalidFields.Count == 0) return null;

        var message = $"Invalid deploy request: {string.Join(", ", invalidFields)}";
        return new InvalidDeployRequestException(message);
    }

    private static string ToJsonValue(string? value) =>
        value == null ? "null" : $"'{value}'";
}