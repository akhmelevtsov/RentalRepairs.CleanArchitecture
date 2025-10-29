namespace RentalRepairs.Application.Common.Interfaces;

/// <summary>
/// ? Generic message delivery abstraction - Issue #15 resolution
/// Provides truly abstract communication interface not tied to specific implementations
/// </summary>
public interface IMessageDeliveryService
{
    Task<DeliveryResult> DeliverAsync(MessageDeliveryRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<DeliveryResult>> DeliverBulkAsync(IEnumerable<MessageDeliveryRequest> requests, CancellationToken cancellationToken = default);
    IEnumerable<DeliveryChannel> SupportedChannels { get; }
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// ? Abstract message delivery request - not tied to email specifics
/// </summary>
public class MessageDeliveryRequest
{
    public string Recipient { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DeliveryChannel Channel { get; init; }
    public MessagePriority Priority { get; init; } = MessagePriority.Normal;
    public Dictionary<string, object> Metadata { get; init; } = new();
    public DateTime? ScheduledDeliveryTime { get; init; }
    public string? Subject { get; init; } // Optional, used when channel supports it
    public MessageFormat Format { get; init; } = MessageFormat.Text;
}

/// <summary>
/// ? Abstract delivery channels - extensible for new communication methods
/// </summary>
public enum DeliveryChannel
{
    Email,
    Sms,
    PushNotification,
    InAppNotification,
    WebSocket,
    Webhook,
    Slack,
    Teams
}

/// <summary>
/// ? Message priority levels for delivery strategies
/// </summary>
public enum MessagePriority
{
    Low,
    Normal,
    High,
    Urgent
}

/// <summary>
/// ? Message format options
/// </summary>
public enum MessageFormat
{
    Text,
    Html,
    Markdown,
    Json
}

/// <summary>
/// ? Abstract delivery result - not tied to email-specific responses
/// </summary>
public class DeliveryResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public string? DeliveryId { get; init; }
    public DateTime DeliveryAttemptedAt { get; init; } = DateTime.UtcNow;
    public DeliveryChannel ChannelUsed { get; init; }
    public Dictionary<string, object> ProviderSpecificData { get; init; } = new();
    public TimeSpan? DeliveryDuration { get; init; }

    public static DeliveryResult Success(DeliveryChannel channel, string? deliveryId = null, TimeSpan? duration = null)
    {
        return new DeliveryResult
        {
            IsSuccess = true,
            ChannelUsed = channel,
            DeliveryId = deliveryId,
            DeliveryDuration = duration
        };
    }

    public static DeliveryResult Failure(DeliveryChannel channel, string errorMessage)
    {
        return new DeliveryResult
        {
            IsSuccess = false,
            ChannelUsed = channel,
            ErrorMessage = errorMessage
        };
    }
}