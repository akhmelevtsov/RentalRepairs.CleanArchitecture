using System.Runtime.CompilerServices;

namespace RentalRepairs.Common.AsyncPatterns;

/// <summary>
/// ? SOLUTION: Standardized async patterns and utilities for consistent async/await usage
/// Addresses Issue #24: Inconsistent Async Patterns
/// </summary>
public static class AsyncPatterns
{
    /// <summary>
    /// ? Use for truly synchronous operations that need to return Task for interface compliance
    /// Better than async/await Task.CompletedTask
    /// </summary>
    public static Task CompletedTask => Task.CompletedTask;

    /// <summary>
    /// ? Use for synchronous operations that return a value but need Task<T> for interface compliance
    /// </summary>
    public static Task<T> FromResult<T>(T result) => Task.FromResult(result);

    /// <summary>
    /// ? ConfigureAwait(false) extension for library code - prevents deadlocks
    /// Use in Infrastructure layer, not in WebUI (which needs synchronization context)
    /// </summary>
    public static ConfiguredTaskAwaitable ConfigureAwaitFalse(this Task task) => task.ConfigureAwait(false);

    /// <summary>
    /// ? ConfigureAwait(false) extension for library code returning values
    /// </summary>
    public static ConfiguredTaskAwaitable<T> ConfigureAwaitFalse<T>(this Task<T> task) => task.ConfigureAwait(false);

    /// <summary>
    /// ? Safe async wrapper for operations that might throw
    /// Prevents unhandled exceptions in fire-and-forget scenarios
    /// </summary>
    public static async Task SafeExecuteAsync(Func<Task> operation, ILogger? logger = null, string? operationName = null)
    {
        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error in async operation: {OperationName}", operationName ?? "Unknown");
            // Don't re-throw - this is for fire-and-forget operations
        }
    }

    /// <summary>
    /// ? Safe async wrapper with return value
    /// </summary>
    public static async Task<T?> SafeExecuteAsync<T>(Func<Task<T>> operation, ILogger? logger = null, string? operationName = null) where T : class
    {
        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error in async operation: {OperationName}", operationName ?? "Unknown");
            return null;
        }
    }
}

/// <summary>
/// ? SOLUTION: Guidelines for consistent async patterns across layers
/// </summary>
public static class AsyncGuidelines
{
    /*
     * ? ASYNC PATTERNS BY LAYER:
     * 
     * 1. WEBUI LAYER (Razor Pages):
     *    - Always use async/await for I/O operations
     *    - Use ConfigureAwait(true) or default (preserve sync context)
     *    - Never use Task.Result or .Wait() - causes deadlocks
     * 
     * 2. APPLICATION LAYER:
     *    - Always use async/await for database, external calls
     *    - Use ConfigureAwait(false) for library code
     *    - Return Task.CompletedTask for sync operations requiring Task return
     * 
     * 3. INFRASTRUCTURE LAYER:
     *    - Always use ConfigureAwait(false) - library code doesn't need sync context
     *    - Use async/await for database, file I/O, network calls
     *    - Never block async code with .Result/.Wait()
     * 
     * 4. DOMAIN LAYER:
     *    - Generally synchronous - domain logic should be fast
     *    - Use async only for domain events that trigger I/O
     */
}

/// <summary>
/// ? Validation attributes for async method signatures
/// </summary>
public static class AsyncValidation
{
    /// <summary>
    /// ? Validates method follows proper async naming convention
    /// </summary>
    public static bool HasProperAsyncNaming(string methodName)
    {
        if (methodName.StartsWith("On") && (methodName.Contains("Get") || methodName.Contains("Post")))
        {
            // Razor Pages handler methods don't need Async suffix
            return true;
        }
        
        return methodName.EndsWith("Async");
    }

    /// <summary>
    /// ? Validates async method has proper return type
    /// </summary>
    public static bool HasProperAsyncReturnType(Type returnType)
    {
        return returnType == typeof(Task) || 
               returnType == typeof(ValueTask) ||
               (returnType.IsGenericType && 
                (returnType.GetGenericTypeDefinition() == typeof(Task<>) || 
                 returnType.GetGenericTypeDefinition() == typeof(ValueTask<>)));
    }
}