namespace Api.Exceptions;

internal sealed class InvalidDeployRequestException : DeployerException
{
    public InvalidDeployRequestException() { }
    public InvalidDeployRequestException(string message) : base(message) { }
    public InvalidDeployRequestException(string message, Exception innerException) : base(message, innerException) { }
}