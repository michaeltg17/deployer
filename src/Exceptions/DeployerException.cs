namespace Api.Exceptions;

public class DeployerException : Exception
{
    public DeployerException(string message) : base(message) { }
    public DeployerException(string message, Exception inner) : base(message, inner) { }
}
