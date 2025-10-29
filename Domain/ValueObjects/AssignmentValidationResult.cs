namespace RentalRepairs.Domain.ValueObjects;

/// <summary>
/// Result of assignment validation with detailed information.
/// Shared value object used across worker assignment domain logic.
/// </summary>
public class AssignmentValidationResult
{
    public bool IsValid { get; }
    public string? ErrorMessage { get; }
    public List<string> Warnings { get; }

    private AssignmentValidationResult(bool isValid, string? errorMessage = null, List<string>? warnings = null)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
        Warnings = warnings ?? new List<string>();
    }

    public static AssignmentValidationResult Success(List<string>? warnings = null) => 
        new(true, null, warnings);

    public static AssignmentValidationResult Failure(string errorMessage) => 
        new(false, errorMessage);
}
