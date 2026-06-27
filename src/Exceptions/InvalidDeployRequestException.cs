using Api.Models;

namespace Api.Exceptions;

public sealed class InvalidDeployRequestException : DeployerException
{
    public DeployRequest Request { get; }

    public InvalidDeployRequestException(DeployRequest request, string message) : base(message)
    {
        Request = request;
    }
}