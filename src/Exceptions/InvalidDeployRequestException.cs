namespace Api.Exceptions;

public sealed class InvalidDeployRequestException : DeployerException
{
    public InvalidDeployRequestException(string message) : base(message) { }
}