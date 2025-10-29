namespace RentalRepairs.Domain.Exceptions;

/// <summary>
/// Base exception for worker assignment domain violations.
/// Follows DDD principles by representing domain rule violations.
/// </summary>
public abstract class WorkerAssignmentDomainException : DomainException
{
    protected WorkerAssignmentDomainException(string message) : base(message) { }
    protected WorkerAssignmentDomainException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when attempting to assign a worker to a request that cannot be assigned.
/// </summary>
public class InvalidRequestForAssignmentException : WorkerAssignmentDomainException
{
    public string CurrentStatus { get; }
    public Guid RequestId { get; }

    public InvalidRequestForAssignmentException(Guid requestId, string currentStatus) 
        : base($"Request {requestId} with status '{currentStatus}' cannot have workers assigned")
    {
        RequestId = requestId;
        CurrentStatus = currentStatus;
    }
}

/// <summary>
/// Exception thrown when attempting to assign a worker to a request that already has a worker assigned.
/// </summary>
public class RequestAlreadyAssignedException : WorkerAssignmentDomainException
{
    public Guid RequestId { get; }
    public string AssignedWorkerEmail { get; }

    public RequestAlreadyAssignedException(Guid requestId, string assignedWorkerEmail) 
        : base($"Request {requestId} is already assigned to worker {assignedWorkerEmail}")
    {
        RequestId = requestId;
        AssignedWorkerEmail = assignedWorkerEmail;
    }
}

/// <summary>
/// Exception thrown when assignment parameters are invalid.
/// </summary>
public class InvalidAssignmentParametersException : WorkerAssignmentDomainException
{
    public string ParameterName { get; }
    public object? ParameterValue { get; }

    public InvalidAssignmentParametersException(string parameterName, object? parameterValue, string reason) 
        : base($"Invalid assignment parameter '{parameterName}': {reason}")
    {
        ParameterName = parameterName;
        ParameterValue = parameterValue;
    }
}

/// <summary>
/// Exception thrown when a worker is not available for assignment.
/// </summary>
public class WorkerNotAvailableException : WorkerAssignmentDomainException
{
    public string WorkerEmail { get; }
    public string Reason { get; }

    public WorkerNotAvailableException(string workerEmail, string reason) 
        : base($"Worker {workerEmail} is not available for assignment: {reason}")
    {
        WorkerEmail = workerEmail;
        Reason = reason;
    }
}
