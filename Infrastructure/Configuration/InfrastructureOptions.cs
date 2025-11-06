using System.ComponentModel.DataAnnotations;

namespace RentalRepairs.Infrastructure.Configuration;

/// <summary>
/// ? Consolidated infrastructure configuration to reduce configuration class explosion
/// Provides unified configuration approach for all infrastructure concerns
/// </summary>
public class InfrastructureOptions
{
    public const string SectionName = "Infrastructure";

    /// <summary>
    /// Database configuration options
    /// </summary>
    public DatabaseOptions Database { get; set; } = new();

    /// <summary>
    /// Email service configuration options
    /// </summary>
    public EmailOptions Email { get; set; } = new();

    /// <summary>
    /// Authentication service configuration options
    /// </summary>
    public AuthenticationOptions Authentication { get; set; } = new();

    /// <summary>
    /// Performance and caching configuration options
    /// </summary>
    public PerformanceOptions Performance { get; set; } = new();
}

/// <summary>
/// Database-specific configuration options
/// </summary>
public class DatabaseOptions
{
    [Required] public string? ConnectionString { get; set; }

    public bool EnableRetry { get; set; } = true;

    [Range(5, 300)] public int CommandTimeoutSeconds { get; set; } = 30;

    [Range(1, 10)] public int MaxRetryCount { get; set; } = 3;

    public bool EnableSensitiveDataLogging { get; set; } = false;

    public bool EnableDetailedErrors { get; set; } = false;
}

/// <summary>
/// Simplified audit configuration options
/// </summary>
public class AuditOptions
{
    /// <summary>
    /// Enable or disable the audit system globally
    /// </summary>
    public bool EnableAuditing { get; set; } = true;

    /// <summary>
    /// Enable soft deletion instead of hard deletion
    /// </summary>
    public bool EnableSoftDelete { get; set; } = true;

    /// <summary>
    /// Only audit properties that have actually changed
    /// </summary>
    public bool AuditOnlyChangedProperties { get; set; } = true;

    /// <summary>
    /// Default user name when current user cannot be determined
    /// </summary>
    public string DefaultAuditUser { get; set; } = "system";

    /// <summary>
    /// Enable detailed audit logging for debugging
    /// </summary>
    public bool EnableDetailedAuditLog { get; set; } = false;

    /// <summary>
    /// Enable concurrency control through row versioning
    /// </summary>
    public bool EnableConcurrencyControl { get; set; } = true;

    /// <summary>
    /// Maximum number of audit entries to keep per entity (0 = unlimited)
    /// </summary>
    public int MaxAuditEntriesPerEntity { get; set; } = 0;

    /// <summary>
    /// Enable automatic cleanup of old audit entries
    /// </summary>
    public bool EnableAuditCleanup { get; set; } = false;

    /// <summary>
    /// Number of days to retain audit entries (when cleanup is enabled)
    /// </summary>
    public int AuditRetentionDays { get; set; } = 365;
}

/// <summary>
/// ? Simplified email configuration without complex provider switching
/// </summary>
public class EmailOptions
{
    public string Provider { get; set; } = "Mock"; // "Mock", "Smtp", "SendGrid"

    [EmailAddress] public string DefaultSender { get; set; } = "noreply@rentalrepairs.com";

    public string DefaultSenderName { get; set; } = "Rental Repairs System";

    public SmtpOptions Smtp { get; set; } = new();

    public SendGridOptions SendGrid { get; set; } = new();

    public bool EnableEmailNotifications { get; set; } = true;
}

/// <summary>
/// SMTP email configuration
/// </summary>
public class SmtpOptions
{
    public string Host { get; set; } = "localhost";

    [Range(1, 65535)] public int Port { get; set; } = 587;

    public bool EnableSsl { get; set; } = true;

    public bool EnableAuthentication { get; set; } = true;

    public string? Username { get; set; }

    public string? Password { get; set; }
}

/// <summary>
/// SendGrid email configuration
/// </summary>
public class SendGridOptions
{
    public string? ApiKey { get; set; }
}

/// <summary>
/// Authentication service configuration
/// </summary>
public class AuthenticationOptions
{
    public string DefaultAuditUser { get; set; } = "system";

    [Range(1, 24)] public int TokenExpirationHours { get; set; } = 8;

    public bool RequireComplexPasswords { get; set; } = false;

    [Range(1, 10)] public int MaxLoginAttempts { get; set; } = 5;

    [Range(5, 60)] public int LockoutDurationMinutes { get; set; } = 15;
}

/// <summary>
/// Performance and caching configuration
/// </summary>
public class PerformanceOptions
{
    public bool EnableCaching { get; set; } = true;

    public string CacheProvider { get; set; } = "Memory"; // "Memory", "Redis"

    [Range(1, 3600)] public int DefaultCacheExpirationMinutes { get; set; } = 30;

    public bool EnablePerformanceMonitoring { get; set; } = true;

    public bool EnableDetailedLogging { get; set; } = false;
}