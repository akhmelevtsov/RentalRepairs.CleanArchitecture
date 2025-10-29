using Microsoft.EntityFrameworkCore;

namespace RentalRepairs.Application.Common.Interfaces;

/// <summary>
/// ? Enhanced domain event publisher interface
/// Handles domain event publishing with proper transaction boundary management
/// </summary>
public interface IDomainEventPublisher
{
    /// <summary>
    /// Publish all pending domain events for entities in the given DbContext
    /// </summary>
    Task PublishEventsAsync(DbContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publish a specific domain event immediately
    /// </summary>
    Task PublishEventAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) 
        where TEvent : class;

    /// <summary>
    /// Get count of pending domain events in the context
    /// </summary>
    int GetPendingEventCount(DbContext context);
}