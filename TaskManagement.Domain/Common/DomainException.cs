namespace TaskManagement.Domain.Common;

/// <summary>Thrown when an operation would break a business rule of the domain.</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
