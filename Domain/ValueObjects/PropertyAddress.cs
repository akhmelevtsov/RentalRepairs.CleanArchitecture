using System.Text.RegularExpressions;
using RentalRepairs.Domain.Common;

namespace RentalRepairs.Domain.ValueObjects;

/// <summary>
/// Self-validating value object that represents a property address.
/// Serves as the single source of truth for property address validation rules.
/// </summary>
public sealed class PropertyAddress : ValueObject
{
    #region Private Fields

    private static readonly Regex _streetNumberRegex = new(@"\d", RegexOptions.Compiled);
    private static readonly Regex _postalCodeRegex = new(@"^[A-Za-z0-9\s\-]{3,10}$", RegexOptions.Compiled);

    #endregion

    #region Properties

    public string StreetNumber { get; private set; } = string.Empty;
    public string StreetName { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string PostalCode { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the formatted full address.
    /// </summary>
    public string FullAddress => $"{StreetNumber} {StreetName}, {City}, {PostalCode}";

    #endregion

    #region Constructors

    // Parameterless constructor for EF Core
    private PropertyAddress()
    {
        // Properties are initialized with default values above
    }

    public PropertyAddress(string streetNumber, string streetName, string city, string postalCode)
    {
        // Single source of truth - validation at construction
        ValidateAndAssign(streetNumber, streetName, city, postalCode);
    }

    #endregion

    #region Public Business Methods

    /// <summary>
    /// Creates address suitable for mailing labels.
    /// </summary>
    /// <returns>The mailing address formatted with line breaks.</returns>
    public string GetMailingAddress()
    {
        return $"{StreetNumber} {StreetName}\n{City}, {PostalCode}";
    }

    /// <summary>
    /// Checks if address is within service area.
    /// </summary>
    /// <param name="serviceCities">The list of cities within the service area.</param>
    /// <returns>True if the address is within the service area.</returns>
    public bool IsWithinServiceArea(List<string> serviceCities)
    {
        return serviceCities.Contains(City, StringComparer.OrdinalIgnoreCase);
    }

    #endregion

    #region Immutable Factory Methods

    /// <summary>
    /// Creates a new instance with different street address while preserving other values.
    /// </summary>
    /// <param name="streetNumber">The new street number.</param>
    /// <param name="streetName">The new street name.</param>
    /// <returns>A new PropertyAddress instance.</returns>
    public PropertyAddress WithStreetAddress(string streetNumber, string streetName)
    {
        return new PropertyAddress(streetNumber, streetName, City, PostalCode);
    }

    /// <summary>
    /// Creates a new instance with different city while preserving other values.
    /// </summary>
    /// <param name="city">The new city.</param>
    /// <returns>A new PropertyAddress instance.</returns>
    public PropertyAddress WithCity(string city)
    {
        return new PropertyAddress(StreetNumber, StreetName, city, PostalCode);
    }

    /// <summary>
    /// Creates a new instance with different postal code while preserving other values.
    /// </summary>
    /// <param name="postalCode">The new postal code.</param>
    /// <returns>A new PropertyAddress instance.</returns>
    public PropertyAddress WithPostalCode(string postalCode)
    {
        return new PropertyAddress(StreetNumber, StreetName, City, postalCode);
    }

    #endregion

    #region Private Validation Methods

    private void ValidateAndAssign(string streetNumber, string streetName, string city, string postalCode)
    {
        StreetNumber = ValidateStreetNumber(streetNumber);
        StreetName = ValidateStreetName(streetName);
        City = ValidateCity(city);
        PostalCode = ValidatePostalCode(postalCode);
    }

    /// <summary>
    /// Domain validation - single source of truth for street number rules.
    /// </summary>
    private static string ValidateStreetNumber(string streetNumber)
    {
        if (string.IsNullOrWhiteSpace(streetNumber))
        {
            throw new ArgumentException("Street number cannot be empty", nameof(streetNumber));
        }

        streetNumber = streetNumber.Trim();

        if (streetNumber.Length > 10)
        {
            throw new ArgumentException("Street number cannot exceed 10 characters", nameof(streetNumber));
        }

        // Street numbers should contain at least one digit (not pure alphabetic)
        if (!_streetNumberRegex.IsMatch(streetNumber))
        {
            throw new ArgumentException("Street number must contain at least one digit", nameof(streetNumber));
        }

        return streetNumber;
    }

    /// <summary>
    /// Domain validation - single source of truth for street name rules.
    /// </summary>
    private static string ValidateStreetName(string streetName)
    {
        if (string.IsNullOrWhiteSpace(streetName))
        {
            throw new ArgumentException("Street name cannot be empty", nameof(streetName));
        }

        streetName = streetName.Trim();

        if (streetName.Length > 100)
        {
            throw new ArgumentException("Street name cannot exceed 100 characters", nameof(streetName));
        }

        return streetName;
    }

    /// <summary>
    /// Domain validation - single source of truth for city rules.
    /// </summary>
    private static string ValidateCity(string city)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            throw new ArgumentException("City cannot be empty", nameof(city));
        }

        city = city.Trim();

        if (city.Length > 50)
        {
            throw new ArgumentException("City cannot exceed 50 characters", nameof(city));
        }

        return city;
    }

    /// <summary>
    /// Domain validation - single source of truth for postal code rules.
    /// </summary>
    private static string ValidatePostalCode(string postalCode)
    {
        if (string.IsNullOrWhiteSpace(postalCode))
        {
            throw new ArgumentException("Postal code cannot be empty", nameof(postalCode));
        }

        postalCode = postalCode.Trim();

        // Basic postal code validation - supports various formats, preserves original case
        if (!_postalCodeRegex.IsMatch(postalCode))
        {
            throw new ArgumentException(
                "Postal code must be 3-10 characters containing letters, numbers, spaces, or hyphens",
                nameof(postalCode));
        }

        return postalCode;
    }

    #endregion

    #region ValueObject Implementation

    /// <summary>
    /// Implements equality comparison using ValueObject base class pattern.
    /// </summary>
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return StreetNumber;
        yield return StreetName;
        yield return City;
        yield return PostalCode;
    }

    public override string ToString()
    {
        return FullAddress;
    }

    #endregion
}
