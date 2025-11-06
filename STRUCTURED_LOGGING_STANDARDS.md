# Structured Logging Standards - Application Layer

## Standard Logging Pattern

### Use Structured Logging with Parameters
```csharp
// ? GOOD - Structured logging with parameters
_logger.LogInformation(
    "User {UserId} performed {Action} on {Resource} at {Timestamp}",
    userId, action, resourceId, DateTime.UtcNow);

// ? BAD - String interpolation (doesn't structure data)
_logger.LogInformation($"User {userId} performed {action}");

// ? BAD - String concatenation
_logger.LogInformation("User " + userId + " performed " + action);
```

### Log Level Usage

#### LogInformation
- Successful operations
- Important state changes
- User actions (login, logout, create, update)
```csharp
_logger.LogInformation(
    "Request {RequestId} completed successfully for user {UserId}",
    requestId, userId);
```

#### LogWarning
- Recoverable errors
- Missing data (but operation continues)
- Slow operations
- Deprecated code usage
```csharp
_logger.LogWarning(
    "User role not found for {UserId} in request {RequestId}",
    userId, requestId);
```

#### LogError
- Exceptions
- Failed operations
- Data integrity issues
```csharp
_logger.LogError(ex,
    "Failed to process {Operation} for {ResourceId}",
    operation, resourceId);
```

#### LogDebug
- Detailed diagnostic information
- Variable values
- Method entry/exit
```csharp
_logger.LogDebug(
    "Processing {ItemCount} items in batch {BatchId}",
    items.Count, batchId);
```

## Standard Patterns by Component

### Services
```csharp
public class MyService
{
    private readonly ILogger<MyService> _logger;

    public async Task<Result> PerformOperationAsync(Guid id, CancellationToken ct)
    {
 _logger.LogInformation(
      "Starting {Operation} for {ResourceId}",
            nameof(PerformOperationAsync), id);

        try
        {
  // ... operation logic

  _logger.LogInformation(
  "{Operation} completed successfully for {ResourceId}",
            nameof(PerformOperationAsync), id);
        }
  catch (Exception ex)
        {
            _logger.LogError(ex,
                "{Operation} failed for {ResourceId}",
                nameof(PerformOperationAsync), id);
      throw;
 }
    }
}
```

### Command Handlers
```csharp
public class CreateEntityCommandHandler : IRequestHandler<CreateEntityCommand, Guid>
{
    private readonly ILogger<CreateEntityCommandHandler> _logger;

    public async Task<Guid> Handle(CreateEntityCommand request, CancellationToken ct)
    {
     _logger.LogInformation(
 "Creating entity with name {EntityName}",
     request.Name);

        try
        {
      // ... creation logic

            _logger.LogInformation(
     "Entity {EntityId} created successfully",
       entity.Id);

            return entity.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
             "Failed to create entity with name {EntityName}",
        request.Name);
          throw;
        }
    }
}
```

### Query Handlers
```csharp
public class GetEntityQueryHandler : IRequestHandler<GetEntityQuery, EntityDto>
{
    private readonly ILogger<GetEntityQueryHandler> _logger;

    public async Task<EntityDto> Handle(GetEntityQuery request, CancellationToken ct)
    {
        _logger.LogDebug(
 "Fetching entity {EntityId}",
            request.EntityId);

      var result = await _repository.GetByIdAsync(request.EntityId, ct);

        if (result == null)
   {
      _logger.LogWarning(
          "Entity {EntityId} not found",
      request.EntityId);
        }

        return result;
    }
}
```

### Event Handlers
```csharp
public class EntityCreatedEventHandler : INotificationHandler<EntityCreatedEvent>
{
    private readonly ILogger<EntityCreatedEventHandler> _logger;

    public async Task Handle(EntityCreatedEvent notification, CancellationToken ct)
    {
        _logger.LogInformation(
 "Handling EntityCreatedEvent for {EntityId}",
       notification.EntityId);

        try
    {
            // ... event handling logic

      _logger.LogInformation(
                "EntityCreatedEvent handled successfully for {EntityId}",
       notification.EntityId);
        }
        catch (Exception ex)
 {
        _logger.LogError(ex,
             "Failed to handle EntityCreatedEvent for {EntityId}",
                notification.EntityId);
          // Decide: rethrow or swallow based on criticality
 }
    }
}
```

## Common Anti-Patterns to Avoid

### 1. String Interpolation
```csharp
// ? BAD
_logger.LogInformation($"User {userId} logged in");

// ? GOOD
_logger.LogInformation("User {UserId} logged in", userId);
```

### 2. Missing Context
```csharp
// ? BAD
_logger.LogInformation("Operation completed");

// ? GOOD
_logger.LogInformation(
    "{Operation} completed for {ResourceId}",
    nameof(MyOperation), resourceId);
```

### 3. Sensitive Data in Logs
```csharp
// ? BAD - Logging passwords, tokens
_logger.LogInformation("User {UserId} logged in with password {Password}", userId, password);

// ? GOOD - Never log sensitive data
_logger.LogInformation("User {UserId} logged in successfully", userId);
```

### 4. Excessive Logging
```csharp
// ? BAD - Too verbose
_logger.LogInformation("Entered method");
_logger.LogInformation("Got parameter {Param}", param);
_logger.LogInformation("Calling repository");
_logger.LogInformation("Got result");
_logger.LogInformation("Returning result");

// ? GOOD - Key milestones only
_logger.LogInformation(
    "Processing {Operation} for {ResourceId}",
    operationName, resourceId);
```

### 5. Catching and Rethrowing Without Adding Value
```csharp
// ? BAD
try
{
    // ... logic
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error occurred");
    throw; // Why catch if just rethrowing?
}

// ? GOOD - Either handle or let it bubble
public async Task OperationAsync()
{
    // No try-catch - let exceptions bubble to behavior pipeline
    return await _repository.SaveAsync();
}
```

## Correlation IDs

For tracking requests across layers:

```csharp
_logger.BeginScope(new Dictionary<string, object>
{
    ["CorrelationId"] = correlationId,
    ["RequestId"] = requestId,
    ["UserId"] = userId
});
```

## Performance Logging

Use structured parameters for performance metrics:

```csharp
_logger.LogWarning(
 "Slow operation: {Operation} took {ElapsedMs}ms for {ResourceId}",
    operationName, elapsed.TotalMilliseconds, resourceId);
```

## Summary

**Key Principles**:
1. ? Always use structured logging with parameters
2. ? Include relevant context (IDs, names, states)
3. ? Use appropriate log levels
4. ? Never log sensitive data
5. ? Add value with each log statement
6. ? Keep logs concise and meaningful
