namespace RentalRepairs.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when a forbidden action is attempted.
/// Follows Clean Architecture patterns from Jason Taylor's template.
/// </summary>
public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException() : base() { }

    public ForbiddenAccessException(string message)
        : base(message)
    {
    }

    public ForbiddenAccessException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}