namespace RentalRepairs.Domain.Services;

/// <summary>
/// Domain service for generating tenant request titles based on business rules.
/// Moves title generation logic from Application layer to Domain layer.
/// Provides consistent and intelligent title generation across the system.
/// </summary>
public class RequestTitleGenerator
{
    // Business rules - title generation constants
    private const int MaxTitleLength = 50;
    private const int MinMeaningfulLength = 10;

    // Common keywords that indicate specific types of requests
    private static readonly Dictionary<string[], string> _commonPatterns = new()
    {
        [new[] { "leak", "leaking", "water", "drip", "pipe" }] = "Water/Plumbing Issue",
        [new[] { "heat", "heating", "cold", "hvac", "temperature", "thermostat" }] = "Heating/Cooling Issue",
        [new[] { "electric", "electrical", "power", "outlet", "switch", "light" }] = "Electrical Issue",
        [new[] { "door", "lock", "key", "handle", "stuck" }] = "Door/Lock Issue",
        [new[] { "window", "glass", "broken", "crack" }] = "Window Issue",
        [new[] { "appliance", "refrigerator", "stove", "dishwasher", "washer", "dryer" }] = "Appliance Issue",
        [new[] { "noise", "loud", "neighbor", "sound" }] = "Noise Issue",
        [new[] { "pest", "bug", "insect", "mouse", "rat", "roach" }] = "Pest Control",
        [new[] { "paint", "wall", "ceiling", "floor", "carpet" }] = "Interior Maintenance",
        [new[] { "emergency", "urgent", "immediate", "asap" }] = "Emergency Request"
    };

    /// <summary>
    /// Business rule: Generates intelligent title from problem description.
    /// Moved from Application layer TenantRequestSubmissionService.
    /// </summary>
    public string GenerateTitle(string problemDescription, string? existingTitle = null)
    {
        // If existing title is provided and adequate, use it
        if (!string.IsNullOrWhiteSpace(existingTitle) && 
            existingTitle.Trim().Length >= MinMeaningfulLength)
        {
            return CleanAndTruncateTitle(existingTitle);
        }

        if (string.IsNullOrWhiteSpace(problemDescription))
        {
            return "Maintenance Request";
        }

        string cleanDescription = problemDescription.Trim();

        // Try pattern-based title generation first
        string patternBasedTitle = GenerateTitleFromPattern(cleanDescription);
        if (!string.IsNullOrEmpty(patternBasedTitle))
        {
            return patternBasedTitle;
        }

        // Try sentence-based extraction
        string sentenceBasedTitle = ExtractTitleFromSentence(cleanDescription);
        if (!string.IsNullOrEmpty(sentenceBasedTitle))
        {
            return sentenceBasedTitle;
        }

        // Fallback to truncated description
        return GenerateTitleFromDescription(cleanDescription);
    }

    /// <summary>
    /// Business rule: Generates title based on urgency level and description.
    /// Enhanced title generation with urgency context.
    /// </summary>
    public string GenerateTitleWithUrgency(string problemDescription, string urgencyLevel, string? existingTitle = null)
    {
        string baseTitle = GenerateTitle(problemDescription, existingTitle);

        // Business rule: Prefix emergency and critical requests
        if (urgencyLevel == "Emergency")
        {
            return $"EMERGENCY: {baseTitle}";
        }

        if (urgencyLevel == "Critical")
        {
            return $"CRITICAL: {baseTitle}";
        }

        return baseTitle;
    }

    /// <summary>
    /// Business rule: Validates if a title meets business requirements.
    /// Ensures title quality standards.
    /// </summary>
    public TitleValidationResult ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return TitleValidationResult.Failure("Title cannot be empty");
        }

        string cleanTitle = title.Trim();

        if (cleanTitle.Length < 3)
        {
            return TitleValidationResult.Failure("Title must be at least 3 characters long");
        }

        if (cleanTitle.Length > 200) // From TenantRequest entity validation
        {
            return TitleValidationResult.Failure("Title cannot exceed 200 characters");
        }

        // Business rule: Title should be meaningful (not just generic phrases)
        string[] genericTitles = new[] { "request", "issue", "problem", "help", "fix", "repair" };
        if (genericTitles.Any(generic => cleanTitle.Equals(generic, StringComparison.OrdinalIgnoreCase)))
        {
            return TitleValidationResult.Warning("Title should be more specific about the issue");
        }

        return TitleValidationResult.Success();
    }

    /// <summary>
    /// Business rule: Suggests improvements for a given title.
    /// Provides intelligent title enhancement recommendations.
    /// </summary>
    public List<string> SuggestTitleImprovements(string currentTitle, string description)
    {
        var suggestions = new List<string>();

        if (string.IsNullOrWhiteSpace(currentTitle) || currentTitle.Trim().Length < MinMeaningfulLength)
        {
            string generatedTitle = GenerateTitle(description);
            suggestions.Add($"Suggested: {generatedTitle}");
        }

        // Check for pattern-based improvements
        string patternTitle = GenerateTitleFromPattern(description);
        if (!string.IsNullOrEmpty(patternTitle) && 
            !patternTitle.Equals(currentTitle?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            suggestions.Add($"Pattern-based: {patternTitle}");
        }

        return suggestions;
    }

    #region Private Helper Methods

    /// <summary>
    /// Generates title based on common problem patterns.
    /// </summary>
    private string GenerateTitleFromPattern(string description)
    {
        string lowerDescription = description.ToLowerInvariant();

        foreach (KeyValuePair<string[], string> pattern in _commonPatterns)
        {
            if (pattern.Key.Any(keyword => lowerDescription.Contains(keyword)))
            {
                // Find the specific keyword that matched
                string matchedKeyword = pattern.Key.First(keyword => lowerDescription.Contains(keyword));

                // Try to extract more context around the keyword
                string contextTitle = ExtractContextAroundKeyword(description, matchedKeyword);
                
                return !string.IsNullOrEmpty(contextTitle) ? contextTitle : pattern.Value;
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Extracts title from the first meaningful sentence.
    /// </summary>
    private string ExtractTitleFromSentence(string description)
    {
        string[] sentences = description.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        
        if (sentences.Length > 0)
        {
            string firstSentence = sentences[0].Trim();
            if (firstSentence.Length >= MinMeaningfulLength && firstSentence.Length <= MaxTitleLength)
            {
                return firstSentence;
            }
            
            if (firstSentence.Length > MaxTitleLength)
            {
                return firstSentence.Substring(0, MaxTitleLength - 3) + "...";
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Generates title by truncating description intelligently.
    /// </summary>
    private string GenerateTitleFromDescription(string description)
    {
        if (description.Length <= MaxTitleLength)
        {
            return description;
        }

        // Try to find a good breaking point (space, comma, etc.)
        int truncationPoint = MaxTitleLength - 3;
        int breakPoint = description.LastIndexOfAny(new[] { ' ', ',', ';' }, truncationPoint);
        
        if (breakPoint > MinMeaningfulLength)
        {
            return description.Substring(0, breakPoint) + "...";
        }

        return description.Substring(0, truncationPoint) + "...";
    }

    /// <summary>
    /// Extracts contextual information around a matched keyword.
    /// </summary>
    private string ExtractContextAroundKeyword(string description, string keyword)
    {
        int index = description.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
        if (index == -1)
        {
            return string.Empty;
        }

        // Try to extract a meaningful phrase around the keyword
        int start = Math.Max(0, index - 20);
        int length = Math.Min(MaxTitleLength, description.Length - start);

        string context = description.Substring(start, length).Trim();

        // Find sentence boundaries within the context
        string[] sentences = context.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (string sentence in sentences)
        {
            if (sentence.ToLowerInvariant().Contains(keyword) && 
                sentence.Trim().Length >= MinMeaningfulLength)
            {
                return CleanAndTruncateTitle(sentence.Trim());
            }
        }

        return CleanAndTruncateTitle(context);
    }

    /// <summary>
    /// Cleans and truncates title to meet business requirements.
    /// </summary>
    private string CleanAndTruncateTitle(string title)
    {
        string cleaned = title.Trim();
        
        // Remove multiple spaces
        while (cleaned.Contains("  "))
        {
            cleaned = cleaned.Replace("  ", " ");
        }

        if (cleaned.Length <= MaxTitleLength)
        {
            return cleaned;
        }

        // Find a good truncation point
        int truncationPoint = MaxTitleLength - 3;
        int lastSpace = cleaned.LastIndexOf(' ', truncationPoint);
        
        if (lastSpace > MinMeaningfulLength)
        {
            return cleaned.Substring(0, lastSpace) + "...";
        }

        return cleaned.Substring(0, truncationPoint) + "...";
    }

    #endregion
}

/// <summary>
/// Result of title validation with business rules feedback.
/// </summary>
public class TitleValidationResult
{
    public bool IsValid { get; }
    public bool HasWarnings { get; }
    public string? Message { get; }
    
    private TitleValidationResult(bool isValid, bool hasWarnings, string? message)
    {
        IsValid = isValid;
        HasWarnings = hasWarnings;
        Message = message;
    }
    
    public static TitleValidationResult Success() => new(true, false, null);
    public static TitleValidationResult Warning(string message) => new(true, true, message);
    public static TitleValidationResult Failure(string message) => new(false, false, message);
}
