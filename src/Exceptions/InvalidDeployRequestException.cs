namespace Api.Exceptions;

internal sealed class InvalidDeployRequestException(string message) : DeployerException(message)
{
}