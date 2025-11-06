# Explicit Interface Implementation Pattern for Audit Fields

## ?? Educational Reference

This document explains the **Explicit Interface Implementation** pattern used in `BaseEntity` to protect audit field integrity while maintaining infrastructure access.

---

## ?? The Problem

**Requirement**: Audit fields must be:
1. ? **Read-only** to domain code (prevent tampering)
2. ? **Writable** by infrastructure layer (EF Core, interceptors)
3. ? **Interface-compliant** (implement `IAuditableEntity`, etc.)

**Challenge**: Standard properties with setters allow anyone to modify audit data.

---

## ?? The Solution: Explicit Interface Implementation

### **Pattern Overview**

```csharp
public abstract class BaseEntity : IAuditableEntity
{
    // Private backing field
  private DateTime _createdAt;
    
    // Public read-only property (domain access)
    public DateTime CreatedAt => _createdAt;
    
    // Explicit interface implementation (infrastructure access)
    DateTime IAuditableEntity.CreatedAt
    {
        get => _createdAt;
        set => _createdAt = value;
    }
}
```

### **How It Works**

1. **Domain Code** sees only the read-only property:
   ```csharp
   var date = entity.CreatedAt; // ? Works
   entity.CreatedAt = DateTime.Now; // ? Compile error - property is read-only
   ```

2. **Infrastructure Code** casts to interface to write:
   ```csharp
   ((IAuditableEntity)entity).CreatedAt = DateTime.UtcNow; // ? Works
   ```

3. **EF Core** uses the interface implementation automatically:
   ```csharp
   // EF Core internally does this when loading from database
   ((IAuditableEntity)entity).CreatedAt = reader.GetDateTime("CreatedAt");
   ```

---

## ??? Complete Implementation

### **Step 1: Define Interface**

```csharp
public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    string CreatedBy { get; set; }
    DateTime? UpdatedAt { get; set; }
    string? UpdatedBy { get; set; }
}
```

### **Step 2: Implement in BaseEntity**

```csharp
public abstract class BaseEntity : IAuditableEntity
{
    // Private backing fields
    private DateTime _createdAt;
    private string _createdBy = string.Empty;
    private DateTime? _updatedAt;
    private string? _updatedBy;

    // Constructor initializes audit fields
    protected BaseEntity()
    {
        Id = Guid.NewGuid();
        _createdAt = DateTime.UtcNow;
     _updatedAt = DateTime.UtcNow;
    }

    // Public read-only properties (domain access)
    public DateTime CreatedAt => _createdAt;
    public string CreatedBy => _createdBy;
    public DateTime? UpdatedAt => _updatedAt;
    public string? UpdatedBy => _updatedBy;

    // Explicit interface implementation (infrastructure access)
    DateTime IAuditableEntity.CreatedAt
    {
        get => _createdAt;
        set => _createdAt = value;
    }

 string IAuditableEntity.CreatedBy
    {
   get => _createdBy;
        set => _createdBy = value ?? string.Empty;
    }

    DateTime? IAuditableEntity.UpdatedAt
    {
 get => _updatedAt;
        set => _updatedAt = value;
    }

    string? IAuditableEntity.UpdatedBy
    {
  get => _updatedBy;
        set => _updatedBy = value;
    }
}
```

### **Step 3: Use in Infrastructure Layer**

```csharp
public class ApplicationDbContext : DbContext
{
    private readonly ICurrentUserService _currentUserService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
     ICurrentUserService currentUserService)
        : base(options)
    {
   _currentUserService = currentUserService;
    }

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        // Get current user
        string currentUser = _currentUserService.GetCurrentUser() ?? "system";
        DateTime now = DateTime.UtcNow;

        // Update audit fields via explicit interface
        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
     switch (entry.State)
            {
case EntityState.Added:
         entry.Entity.CreatedAt = now;
   entry.Entity.CreatedBy = currentUser;
      entry.Entity.UpdatedAt = now;
          entry.Entity.UpdatedBy = currentUser;
       break;

  case EntityState.Modified:
           entry.Entity.UpdatedAt = now;
           entry.Entity.UpdatedBy = currentUser;
       break;
            }
        }

        // Handle soft deletes
        foreach (var entry in ChangeTracker.Entries<ISoftDeletableEntity>())
        {
         if (entry.State == EntityState.Deleted)
       {
      entry.State = EntityState.Modified;
       entry.Entity.IsDeleted = true;
    entry.Entity.DeletedAt = now;
           entry.Entity.DeletedBy = currentUser;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
```

---

## ?? Usage Examples

### **Example 1: Domain Code (Read-Only)**

```csharp
public class TenantRequestService
{
  public string GetRequestAuditInfo(TenantRequest request)
    {
        // ? Can read audit fields
   var createdBy = request.CreatedBy;
        var createdAt = request.CreatedAt;
        var updatedAt = request.UpdatedAt;
        
        // ? Cannot modify audit fields - compile error
        // request.CreatedBy = "hacker"; // ERROR!
        // request.CreatedAt = DateTime.Now; // ERROR!
      
  return $"Created by {createdBy} on {createdAt:yyyy-MM-dd}";
    }
}
```

### **Example 2: Infrastructure Code (Full Access)**

```csharp
public class AuditInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
   InterceptionResult<int> result)
    {
        var context = eventData.Context;
        if (context == null) return result;

 foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
 {
            if (entry.State == EntityState.Added)
        {
    // ? Infrastructure can modify via interface
       entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.CreatedBy = "system";
            }
        }

        return result;
    }
}
```

### **Example 3: Unit Testing**

```csharp
[Fact]
public void TenantRequest_CreatedAt_IsReadOnly()
{
    // Arrange
    var request = TenantRequest.CreateNew(/* ... */);
    
// Act - Read is allowed
    var createdAt = request.CreatedAt;
    
// Assert
    Assert.NotNull(createdAt);
    
    // Compile error if you try to set:
    // request.CreatedAt = DateTime.Now; // ERROR!
}

[Fact]
public void Infrastructure_Can_Set_AuditFields()
{
    // Arrange
    var request = TenantRequest.CreateNew(/* ... */);
    IAuditableEntity auditable = request; // Cast to interface
    
    // Act - Infrastructure can modify via interface
    auditable.CreatedBy = "test@example.com";
  auditable.UpdatedAt = DateTime.UtcNow;
    
    // Assert
    Assert.Equal("test@example.com", request.CreatedBy);
    Assert.NotNull(request.UpdatedAt);
}
```

---

## ? Benefits

1. **Security** ??
   - Domain code cannot tamper with audit trails
   - Prevents accidental or malicious modifications

2. **Clean Architecture** ???
   - Clear separation: domain vs. infrastructure concerns
   - Infrastructure layer has necessary control

3. **Type Safety** ???
   - Compile-time enforcement
   - IDE will prevent incorrect usage

4. **EF Core Compatibility** ??
   - EF Core automatically uses interface implementations
   - No special configuration needed

5. **Testability** ??
   - Easy to test domain logic (read-only access)
   - Easy to test infrastructure logic (via interface)

6. **DDD Compliance** ??
   - Follows Domain-Driven Design principles
   - Audit concerns belong to infrastructure, not domain

---

## ?? Anti-Patterns to Avoid

### ? **Anti-Pattern 1: Public Setters**

```csharp
// BAD - Anyone can modify audit fields
public DateTime CreatedAt { get; set; }
public string CreatedBy { get; set; }
```

### ? **Anti-Pattern 2: Internal Setters Without Abstraction**

```csharp
// BAD - Still exposes setters, just limited to assembly
public DateTime CreatedAt { get; internal set; }
```

### ? **Anti-Pattern 3: Protected Setters**

```csharp
// BAD - Derived classes can modify audit fields
public DateTime CreatedAt { get; protected set; }
```

### ? **Correct Pattern: Explicit Interface Implementation**

```csharp
// GOOD - Read-only to domain, writable via interface
private DateTime _createdAt;
public DateTime CreatedAt => _createdAt;

DateTime IAuditableEntity.CreatedAt
{
    get => _createdAt;
    set => _createdAt = value;
}
```

---

## ?? Related Patterns

### **1. Strategy Pattern**
Use explicit interface implementation to provide different behaviors for different callers.

### **2. Interface Segregation Principle (ISP)**
Separate interfaces for different concerns (auditable, soft-deletable, versioned).

### **3. Encapsulation**
Hide implementation details while exposing controlled access.

---

## ?? References

- [Microsoft Docs: Explicit Interface Implementation](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/interfaces/explicit-interface-implementation)
- [Domain-Driven Design by Eric Evans](https://www.domainlanguage.com/ddd/)
- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)

---

## ?? Key Takeaways

1. ? Use explicit interface implementation to separate concerns
2. ? Domain code sees read-only properties
3. ? Infrastructure code accesses via interface
4. ? Compile-time safety prevents audit tampering
5. ? EF Core works seamlessly with this pattern
6. ? Follows DDD and Clean Architecture principles

---

**Pattern Status**: ? **PRODUCTION-READY**  
**Complexity**: ??? **Moderate**  
**Value**: ????? **Very High**

