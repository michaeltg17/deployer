namespace Api.Exceptions;

public sealed class InvalidDeployRequestException(string message) : DeployerException(message)
{
}