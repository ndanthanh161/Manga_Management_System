namespace MMS.Domain.Exceptions;

public class InvalidWorkflowTransitionException : DomainException
{
    public InvalidWorkflowTransitionException(string message) 
        : base("INVALID_WORKFLOW_TRANSITION", message)
    {
    }
}
