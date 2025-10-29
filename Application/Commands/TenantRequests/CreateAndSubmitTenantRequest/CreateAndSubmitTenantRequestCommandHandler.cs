using Microsoft.Extensions.Logging;
using RentalRepairs.Application.Common.Interfaces;

namespace RentalRepairs.Application.Commands.TenantRequests.CreateAndSubmitTenantRequest;

/// <summary>
/// Handler for CreateAndSubmitTenantRequestCommand
/// Demonstrates CQRS command handler with business validation
/// Replaces service method orchestration with command pattern
/// </summary>
public class CreateAndSubmitTenantRequestCommandHandler : ICommandHandler<CreateAndSubmitTenantRequestCommand, CreateAndSubmitTenantRequestResult>
{
    private readonly ILogger<CreateAndSubmitTenantRequestCommandHandler> _logger;

    public CreateAndSubmitTenantRequestCommandHandler(ILogger<CreateAndSubmitTenantRequestCommandHandler> logger)
    {
        _logger = logger;
    }

    public async Task<CreateAndSubmitTenantRequestResult> Handle(CreateAndSubmitTenantRequestCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing CreateAndSubmitTenantRequest for tenant {TenantEmail}", request.TenantEmail);

            // BUSINESS VALIDATION: Input validation
            var validationResult = ValidateRequest(request);
            if (!validationResult.IsValid)
            {
                return new CreateAndSubmitTenantRequestResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Validation failed",
                    ValidationErrors = validationResult.Errors
                };
            }

            // BUSINESS VALIDATION: Rate limiting (unless overridden)
            if (!request.SkipRateLimitValidation)
            {
                var rateLimitResult = await CheckRateLimits(request.TenantEmail, cancellationToken);
                if (!rateLimitResult.IsAllowed)
                {
                    return new CreateAndSubmitTenantRequestResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Rate limit exceeded",
                        ValidationErrors = rateLimitResult.ErrorMessages
                    };
                }
            }

            // BUSINESS LOGIC: Generate request code and assign superintendent
            var requestCode = GenerateRequestCode(request.PropertyCode, request.UrgencyLevel);
            var assignedSuperintendent = DetermineAssignedSuperintendent(request.PropertyCode);
            var estimatedResponse = CalculateEstimatedResponseTime(request.UrgencyLevel);

            // Simulate request creation and submission
            var requestId = Guid.NewGuid();
            
            _logger.LogInformation("Created and submitted tenant request {RequestId} with code {RequestCode}", 
                requestId, requestCode);

            // BUSINESS LOGIC: Generate next steps based on urgency
            var nextSteps = GenerateNextSteps(request.UrgencyLevel);

            return new CreateAndSubmitTenantRequestResult
            {
                IsSuccess = true,
                RequestId = requestId,
                RequestCode = requestCode,
                EstimatedResponseTime = estimatedResponse,
                AssignedSuperintendent = assignedSuperintendent,
                NextSteps = nextSteps
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CreateAndSubmitTenantRequest for tenant {TenantEmail}", request.TenantEmail);
            return new CreateAndSubmitTenantRequestResult
            {
                IsSuccess = false,
                ErrorMessage = $"Internal error: {ex.Message}"
            };
        }
    }

    #region Private Business Logic Methods

    private ValidationResult ValidateRequest(CreateAndSubmitTenantRequestCommand request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Title))
            errors.Add("Title is required");

        if (string.IsNullOrWhiteSpace(request.Description))
            errors.Add("Description is required");

        if (string.IsNullOrWhiteSpace(request.TenantEmail))
            errors.Add("Tenant email is required");

        if (string.IsNullOrWhiteSpace(request.PropertyCode))
            errors.Add("Property code is required");

        if (string.IsNullOrWhiteSpace(request.UnitNumber))
            errors.Add("Unit number is required");

        var validUrgencyLevels = new[] { "Low", "Normal", "High", "Critical", "Emergency" };
        if (!validUrgencyLevels.Contains(request.UrgencyLevel))
            errors.Add($"Invalid urgency level. Must be one of: {string.Join(", ", validUrgencyLevels)}");

        // Business rule: Emergency requests require justification in description
        if (request.UrgencyLevel == "Emergency" && request.Description.Length < 50)
            errors.Add("Emergency requests require detailed description (minimum 50 characters)");

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };
    }

    private async Task<RateLimitResult> CheckRateLimits(string tenantEmail, CancellationToken cancellationToken)
    {
        // Simulate rate limiting business logic
        await Task.Delay(10, cancellationToken);

        // Business rule: Max 3 requests per week per tenant
        // Business rule: Max 1 emergency request per month
        // In production, this would check actual database records

        return new RateLimitResult
        {
            IsAllowed = true,
            ErrorMessages = new List<string>()
        };
    }

    private string GenerateRequestCode(string propertyCode, string urgencyLevel)
    {
        // Business logic: Request code format
        var prefix = urgencyLevel switch
        {
            "Emergency" => "EMG",
            "Critical" => "CRT", 
            "High" => "HGH",
            _ => "REQ"
        };

        var timestamp = DateTime.Now.ToString("yyyyMMddHHmm");
        return $"{prefix}-{propertyCode}-{timestamp}";
    }

    private string DetermineAssignedSuperintendent(string propertyCode)
    {
        // Business logic: Superintendent assignment based on property
        return $"super.{propertyCode.ToLower()}@rentalrepairs.com";
    }

    private DateTime CalculateEstimatedResponseTime(string urgencyLevel)
    {
        // Business logic: Response time SLAs
        var responseHours = urgencyLevel switch
        {
            "Emergency" => 2,
            "Critical" => 4,
            "High" => 24,
            "Normal" => 72,
            "Low" => 168,
            _ => 72
        };

        return DateTime.Now.AddHours(responseHours);
    }

    private List<string> GenerateNextSteps(string urgencyLevel)
    {
        // Business logic: Next steps based on urgency
        return urgencyLevel switch
        {
            "Emergency" => new List<string>
            {
                "Superintendent notified immediately",
                "Emergency response team activated",
                "You will be contacted within 2 hours"
            },
            "Critical" => new List<string>
            {
                "Priority review scheduled",
                "Worker assignment within 4 hours",
                "SMS updates will be sent"
            },
            _ => new List<string>
            {
                "Request submitted to property superintendent",
                "You will receive email updates on progress",
                "Expected response within business hours"
            }
        };
    }

    #endregion

    #region Supporting Classes

    private class ValidationResult
    {
        public bool IsValid { get; init; }
        public List<string> Errors { get; init; } = new();
    }

    private class RateLimitResult
    {
        public bool IsAllowed { get; init; }
        public List<string> ErrorMessages { get; init; } = new();
    }

    #endregion
}