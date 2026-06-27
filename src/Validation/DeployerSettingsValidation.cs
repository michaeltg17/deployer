using Microsoft.Extensions.Options;
using Api.Models;

namespace Api.Validation;

public sealed class DeployerSettingsValidation : IValidateOptions<DeployerSettings>
{
    public ValidateOptionsResult Validate(string? name, DeployerSettings options)
    {
        if (string.IsNullOrEmpty(options.GhcrUser) != string.IsNullOrEmpty(options.GhcrToken))
        {
            return string.IsNullOrEmpty(options.GhcrUser)
                ? ValidateOptionsResult.Fail("GhcrToken is set but GhcrUser is missing")
                : ValidateOptionsResult.Fail("GhcrUser is set but GhcrToken is missing");
        }

        return ValidateOptionsResult.Success;
    }
}
