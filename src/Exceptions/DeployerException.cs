namespace Api.Exceptions;

internal class DeployerException : Exception
{
    public DeployerException() { }
    public DeployerException(string message) : base(message) { }
    public DeployerException(string message, Exception inner) : base(message, inner) { }
}
