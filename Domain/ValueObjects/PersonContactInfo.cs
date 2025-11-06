using System.Text.RegularExpressions;
using RentalRepairs.Domain.Common;

namespace RentalRepairs.Domain.ValueObjects;

/// <summary>
/// Self-validating value object that represents person contact information.
/// Serves as the single source of truth for person contact validation rules.
/// </summary>
public sealed class PersonContactInfo : ValueObject
{
    #region Private Fields and Constants

    private static readonly Regex NameRegex = new(@"^[a-zA-Z\s\-']+$", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex = new(@"^\+?[\d\s\-\(\)]+$", RegexOptions.Compiled);

    #endregion

    #region Properties

    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string EmailAddress { get; private set; } = string.Empty;
    public string? MobilePhone { get; private set; }

    #endregion

    #region Constructors

    // Parameterless constructor for EF Core
    private PersonContactInfo()
    {
        // Properties are initialized with default values above
    }

    public PersonContactInfo(string firstName, string lastName, string emailAddress, string? mobilePhone = null)
    {
        // Single source of truth - validation happens at construction
        ValidateAndAssign(firstName, lastName, emailAddress, mobilePhone);
    }

    #endregion

    #region Public Business Methods

    /// <summary>
    /// Combines first and last name consistently.
    /// </summary>
    /// <returns>The full name in "FirstName LastName" format.</returns>
    public string GetFullName()
    {
        return $"{FirstName} {LastName}";
    }

    /// <summary>
    /// Gets the formal name format for official documents.
    /// </summary>
    /// <returns>The formal name in "LastName, FirstName" format.</returns>
    public string GetFormalName()
    {
        return $"{LastName}, {FirstName}";
    }

    /// <summary>
    /// Gets the initials format.
    /// </summary>
    /// <returns>The initials in "F.L." format.</returns>
    public string GetInitials()
    {
        string firstInitial = FirstName.Length > 0 ? FirstName[0].ToString().ToUpperInvariant() : "";
        string lastInitial = LastName.Length > 0 ? LastName[0].ToString().ToUpperInvariant() : "";
        return $"{firstInitial}.{lastInitial}.";
    }

    /// <summary>
    /// Checks if this contact information is valid for notifications.
    /// </summary>
    /// <returns>True if the email address is valid for notifications.</returns>
    public bool IsValidForNotifications()
    {
        return !string.IsNullOrWhiteSpace(EmailAddress);
    }

    /// <summary>
    /// Creates a display-friendly version including phone number if available.
    /// </summary>
    /// <returns>The display name with optional phone number.</returns>
    public string GetDisplayName()
    {
        string name = GetFullName();
        if (!string.IsNullOrWhiteSpace(MobilePhone))
        {
            name += $" ({MobilePhone})";
        }

        return name;
    }

    #endregion

    #region Immutable Factory Methods

    /// <summary>
    /// Creates a new instance with different name while preserving other values.
    /// </summary>
    /// <param name="firstName">The new first name.</param>
    /// <param name="lastName">The new last name.</param>
    /// <returns>A new PersonContactInfo instance.</returns>
    public PersonContactInfo WithName(string firstName, string lastName)
    {
        return new PersonContactInfo(firstName, lastName, EmailAddress, MobilePhone);
    }

    /// <summary>
    /// Creates a new instance with different email while preserving other values.
    /// </summary>
    /// <param name="emailAddress">The new email address.</param>
    /// <returns>A new PersonContactInfo instance.</returns>
    public PersonContactInfo WithEmail(string emailAddress)
    {
        return new PersonContactInfo(FirstName, LastName, emailAddress, MobilePhone);
    }

    /// <summary>
    /// Creates a new instance with different phone while preserving other values.
    /// </summary>
    /// <param name="mobilePhone">The new mobile phone number.</param>
    /// <returns>A new PersonContactInfo instance.</returns>
    public PersonContactInfo WithPhone(string? mobilePhone)
    {
        return new PersonContactInfo(FirstName, LastName, EmailAddress, mobilePhone);
    }

    #endregion

    #region Private Validation Methods

    private void ValidateAndAssign(string firstName, string lastName, string emailAddress, string? mobilePhone)
    {
        FirstName = ValidateFirstName(firstName);
        LastName = ValidateLastName(lastName);
        EmailAddress = ValidateEmailAddress(emailAddress);
        MobilePhone = ValidateMobilePhone(mobilePhone);
    }

    /// <summary>
    /// Domain validation - single source of truth for first name rules.
    /// </summary>
    private static string ValidateFirstName(string firstName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException("First name cannot be empty", nameof(firstName));
        }

        firstName = firstName.Trim();

        if (firstName.Length < 2)
        {
            throw new ArgumentException("First name must be at least 2 characters", nameof(firstName));
        }

        if (firstName.Length > 50)
        {
            throw new ArgumentException("First name cannot exceed 50 characters", nameof(firstName));
        }

        // Names should only contain letters, spaces, hyphens, and apostrophes
        if (!NameRegex.IsMatch(firstName))
        {
            throw new ArgumentException("First name can only contain letters, spaces, hyphens, and apostrophes",
                nameof(firstName));
        }

        return firstName;
    }

    /// <summary>
    /// Domain validation - single source of truth for last name rules.
    /// </summary>
    private static string ValidateLastName(string lastName)
    {
        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException("Last name cannot be empty", nameof(lastName));
        }

        lastName = lastName.Trim();

        if (lastName.Length < 2)
        {
            throw new ArgumentException("Last name must be at least 2 characters", nameof(lastName));
        }

        if (lastName.Length > 50)
        {
            throw new ArgumentException("Last name cannot exceed 50 characters", nameof(lastName));
        }

        // Names should only contain letters, spaces, hyphens, and apostrophes
        if (!NameRegex.IsMatch(lastName))
        {
            throw new ArgumentException("Last name can only contain letters, spaces, hyphens, and apostrophes",
                nameof(lastName));
        }

        return lastName;
    }

    /// <summary>
    /// Domain validation - single source of truth for email rules.
    /// </summary>
    private static string ValidateEmailAddress(string emailAddress)
    {
        if (string.IsNullOrWhiteSpace(emailAddress))
        {
            throw new ArgumentException("Email address cannot be empty", nameof(emailAddress));
        }

        emailAddress = emailAddress.Trim().ToLowerInvariant();

        // Enhanced email validation
        if (emailAddress.Length > 254) // RFC compliant
        {
            throw new ArgumentException("Email address cannot exceed 254 characters", nameof(emailAddress));
        }

        // Check for basic structure
        if (!emailAddress.Contains("@"))
        {
            throw new ArgumentException("Email address must be valid", nameof(emailAddress));
        }

        string[] parts = emailAddress.Split('@');
        if (parts.Length != 2)
        {
            throw new ArgumentException("Email address must be valid", nameof(emailAddress));
        }

        string localPart = parts[0];
        string domainPart = parts[1];

        // Local part validation
        if (string.IsNullOrEmpty(localPart) || localPart.StartsWith(".") || localPart.EndsWith(".") ||
            localPart.Contains(".."))
        {
            throw new ArgumentException("Email address must be valid", nameof(emailAddress));
        }

        // Domain part validation
        if (string.IsNullOrEmpty(domainPart) || !domainPart.Contains(".") || domainPart.StartsWith(".") ||
            domainPart.EndsWith("."))
        {
            throw new ArgumentException("Email address must be valid", nameof(emailAddress));
        }

        return emailAddress;
    }

    /// <summary>
    /// Domain validation - single source of truth for mobile phone rules.
    /// </summary>
    private static string? ValidateMobilePhone(string? mobilePhone)
    {
        if (string.IsNullOrWhiteSpace(mobilePhone))
        {
            return null;
        }

        mobilePhone = mobilePhone.Trim();

        // Basic phone validation - allows international formats
        if (!PhoneRegex.IsMatch(mobilePhone))
        {
            throw new ArgumentException(
                "Mobile phone must contain only numbers, spaces, hyphens, parentheses, and optional plus sign",
                nameof(mobilePhone));
        }

        if (mobilePhone.Length > 20)
        {
            throw new ArgumentException("Mobile phone cannot exceed 20 characters", nameof(mobilePhone));
        }

        return mobilePhone;
    }

    #endregion

    #region ValueObject Implementation

    /// <summary>
    /// Implements equality comparison using ValueObject base class pattern.
    /// </summary>
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return FirstName;
        yield return LastName;
        yield return EmailAddress;
        yield return MobilePhone ?? string.Empty;
    }

    public override string ToString()
    {
        return GetDisplayName();
    }

    #endregion
}
