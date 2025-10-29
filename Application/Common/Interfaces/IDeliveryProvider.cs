namespace RentalRepairs.Application.Common.Interfaces;

/// <summary>
/// ? Base delivery provider abstraction - Issue #15 resolution
/// Truly abstract interface not tied to specific implementation details
/// </summary>
public interface IDeliveryProvider
{
    DeliveryChannel SupportedChannel { get; }
    string ProviderName { get; }
    Task<DeliveryResult> SendAsync(MessageDeliveryRequest request, CancellationToken cancellationToken = default);
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
    ProviderCapabilities Capabilities { get; }
}

/// <summary>
/// ? Provider capabilities for feature detection
/// </summary>
public class ProviderCapabilities
{
    public bool SupportsScheduling { get; init; }
    public bool SupportsBulkDelivery { get; init; }
    public bool SupportsAttachments { get; init; }
    public bool SupportsHtml { get; init; }
    public bool SupportsTemplating { get; init; }
    public int MaxMessageSize { get; init; } = int.MaxValue;
    public int RateLimitPerSecond { get; init; } = int.MaxValue;
    public List<MessageFormat> SupportedFormats { get; init; } = new();
}

/// <summary>
/// ? Provider registry for managing delivery providers
/// </summary>
public interface IDeliveryProviderRegistry
{
    IDeliveryProvider? GetProvider(DeliveryChannel channel, string? providerName = null);
    IEnumerable<IDeliveryProvider> GetAllProviders(DeliveryChannel channel);
    void RegisterProvider(IDeliveryProvider provider, string? name = null);
    bool IsChannelSupported(DeliveryChannel channel);
    IEnumerable<DeliveryChannel> GetSupportedChannels();
}

/// <summary>
/// ? Delivery strategy abstraction for handling complex delivery logic
/// </summary>
public interface IDeliveryStrategy
{
    Task<DeliveryResult> ExecuteAsync(MessageDeliveryRequest request, IDeliveryProviderRegistry registry, CancellationToken cancellationToken = default);
    string StrategyName { get; }
    bool SupportsChannel(DeliveryChannel channel);
}

/// <summary>
/// ? Email-specific provider interface (when email-specific features needed)
/// Extends base provider while maintaining abstraction
/// </summary>
public interface IEmailDeliveryProvider : IDeliveryProvider
{
    Task<DeliveryResult> SendEmailAsync(EmailDeliveryRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// ? Email-specific request model for advanced email features
/// </summary>
public class EmailDeliveryRequest : MessageDeliveryRequest
{
    public List<string>? CcRecipients { get; init; }
    public List<string>? BccRecipients { get; init; }
    public List<EmailAttachment>? Attachments { get; init; }
    public string? ReplyToEmail { get; init; }
    public Dictionary<string, string>? Headers { get; init; }

    public EmailDeliveryRequest(string recipient, string content, string? subject = null) 
    {
        Recipient = recipient;
        Content = content;
        Subject = subject;
        Channel = DeliveryChannel.Email;
    }

    // Factory method from base request using object initializer syntax
    public static EmailDeliveryRequest FromBase(MessageDeliveryRequest baseRequest)
    {
        return new EmailDeliveryRequest(baseRequest.Recipient, baseRequest.Content, baseRequest.Subject)
        {
            Priority = baseRequest.Priority,
            Metadata = new Dictionary<string, object>(baseRequest.Metadata),
            ScheduledDeliveryTime = baseRequest.ScheduledDeliveryTime,
            Format = baseRequest.Format
        };
    }
}

/// <summary>
/// ? Email attachment model
/// </summary>
public class EmailAttachment
{
    public required string FileName { get; init; }
    public required byte[] Content { get; init; }
    public required string ContentType { get; init; }
    public bool IsInline { get; init; }
    public string? ContentId { get; init; }
}

/// <summary>
/// ? SMS-specific provider interface
/// </summary>
public interface ISmsDeliveryProvider : IDeliveryProvider
{
    Task<DeliveryResult> SendSmsAsync(SmsDeliveryRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// ? SMS-specific request model
/// </summary>
public class SmsDeliveryRequest : MessageDeliveryRequest
{
    public string? FromNumber { get; init; }
    public bool IsUnicode { get; init; }

    public SmsDeliveryRequest(string recipient, string content, string? fromNumber = null)
    {
        Recipient = recipient;
        Content = content;
        Channel = DeliveryChannel.Sms;
        FromNumber = fromNumber;
    }

    // Factory method from base request using object initializer syntax
    public static SmsDeliveryRequest FromBase(MessageDeliveryRequest baseRequest)
    {
        return new SmsDeliveryRequest(baseRequest.Recipient, baseRequest.Content)
        {
            Priority = baseRequest.Priority,
            Metadata = new Dictionary<string, object>(baseRequest.Metadata),
            ScheduledDeliveryTime = baseRequest.ScheduledDeliveryTime
        };
    }
}