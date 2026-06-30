using Microsoft.Extensions.Options;
using Api.Models;

namespace Api.Validation;

public sealed class DeployerSettingsValidator : IValidateOptions<DeployerSettings>
{
    public ValidateOptionsResult Validate(string? name, DeployerSettings options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ImageRepo))
            errors.Add($"The '{nameof(options.ImageRepo)}' setting is required");

        if (string.IsNullOrWhiteSpace(options.KeePassDbPath))
            errors.Add($"The '{nameof(options.KeePassDbPath)}' setting is required");

        if (string.IsNullOrWhiteSpace(options.KeePassDbPassword))
            errors.Add($"The '{nameof(options.KeePassDbPassword)}' setting is required");

        if (errors.Count > 0)
            return ValidateOptionsResult.Fail(errors);

        return ValidateOptionsResult.Success;
    }
}