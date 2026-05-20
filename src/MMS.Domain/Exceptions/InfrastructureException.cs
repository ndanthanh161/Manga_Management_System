namespace MMS.Domain.Exceptions;

public class InfrastructureException : Exception
{
    public string ErrorCode { get; }
    public InfrastructureException(string errorCode, string message, Exception? innerException = null) 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
