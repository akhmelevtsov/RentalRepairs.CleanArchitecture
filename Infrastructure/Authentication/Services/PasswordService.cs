using BC = BCrypt.Net.BCrypt;
using Microsoft.Extensions.Hosting;

namespace RentalRepairs.Infrastructure.Authentication.Services;

/// <summary>
/// Secure password hashing service using BCrypt
/// Provides strong password security for demo and production use
/// </summary>
public interface IPasswordService
{
    /// <summary>
    /// Hash a password using BCrypt with salt
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verify a password against its hash
    /// </summary>
    bool VerifyPassword(string password, string hashedPassword);

    /// <summary>
    /// Check if password meets security requirements
    /// </summary>
    bool IsValidPassword(string password, int minLength = 6);

    /// <summary>
    /// Generate a secure random password for demo purposes
    /// </summary>
    string GenerateSecurePassword(int length = 12);
}

/// <summary>
/// BCrypt-based password service implementation with environment-optimized work factor
/// </summary>
public class PasswordService : IPasswordService
{
    private readonly int _workFactor;

    public PasswordService(IHostEnvironment environment)
    {
        // ? PERFORMANCE OPTIMIZATION: Use different work factors by environment
        _workFactor = environment.IsDevelopment() 
            ? 8   // Fast for development (~50ms)
            : 12; // Secure for production (~500ms-2s)
    }

    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        return BC.HashPassword(password, _workFactor);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hashedPassword))
            return false;

        try
        {
            return BC.Verify(password, hashedPassword);
        }
        catch (Exception)
        {
            // BCrypt verification failed
            return false;
        }
    }

    public bool IsValidPassword(string password, int minLength = 6)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        if (password.Length < minLength)
            return false;

        // Basic password requirements for demo
        // In production, you might want stronger requirements
        return true;
    }

    public string GenerateSecurePassword(int length = 12)
    {
        const string validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var random = new Random();
        
        return new string(Enumerable.Repeat(validChars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}