# Worker Specialization - Domain Layer Analysis

**Date**: 2024  
**Status**: ?? **NEEDS REFACTORING**  
**Issue**: Specialization logic is split across multiple layers

---

## Executive Summary

Worker specialization is currently implemented **inconsistently across 3 layers**:

1. ? **Domain Layer** - `Worker.Specialization` property + validation logic
2. ? **Application Layer** - `WorkerService.DetermineRequiredSpecialization()` with keyword mapping
3. ? **Configuration Layer** - `SpecializationSettings` with keyword lists

**Recommendation**: ? **Consolidate ALL specialization logic into Domain layer**

---

## Current State Analysis

### 1. Domain Layer (Worker Entity)

**File**: `Domain/Entities/Worker.cs`

#### ? What's Correct:
```csharp
public string? Specialization { get; private set; }

public void SetSpecialization(string specialization)
{
    if (string.IsNullOrWhiteSpace(specialization))
      throw new ArgumentException("Specialization cannot be empty");
    
    string? oldSpecialization = Specialization;
    Specialization = specialization;
    
    if (oldSpecialization != specialization)
AddDomainEvent(new WorkerSpecializationChangedEvent(this, oldSpecialization, specialization));
}
```

**? Good**:
- Specialization is encapsulated in Worker aggregate
- Domain events for specialization changes
- Validation logic in domain

#### ? What's Wrong:

**1. Specialization Determination Logic in Domain**
```csharp
// ? PROBLEM: Static method in Worker entity
public static string DetermineRequiredSpecialization(string title, string description)
{
    string text = $"{title} {description}".ToLowerInvariant();

  // Hard-coded keyword matching
    if (ContainsKeywords(text, "plumb", "leak", "water", ...))
        return "Plumbing";
    
    if (ContainsKeywords(text, "electric", "power", ...))
        return "Electrical";
    
    // ... 7 more hard-coded if statements
    
    return "General Maintenance";
}
```

**Problems**:
- ? Hard-coded keyword lists in domain
- ? Business rule changes require code recompilation
- ? Should be configurable, not hard-coded
- ? Violates Open/Closed Principle

**2. Specialization Normalization in Domain**
```csharp
// ? PROBLEM: Hard-coded mapping in domain
private static string NormalizeSpecialization(string specialization)
{
    return specialization.ToLowerInvariant() switch
    {
      "plumber" => "Plumbing",
        "electrician" => "Electrical",
  // ... 12 more hard-coded cases
_ => specialization
    };
}
```

**Problems**:
- ? Hard-coded variations
- ? Can't add new specializations without recompiling
- ? Business configuration in domain code

**3. Specialization Matching Logic in Domain**
```csharp
public bool HasSpecializedSkills(string requiredSpecialization)
{
    if (string.IsNullOrWhiteSpace(requiredSpecialization))
        return true;
    
  if (Specialization.Equals(requiredSpecialization, StringComparison.OrdinalIgnoreCase))
        return true;
    
    // "General Maintenance" can handle anything
    if (Specialization.Equals("General Maintenance", StringComparison.OrdinalIgnoreCase))
     return true;
    
    // Normalization matching
  string normalizedWorkerSpec = NormalizeSpecialization(Specialization);
    string normalizedRequiredSpec = NormalizeSpecialization(requiredSpecialization);
    
    return normalizedWorkerSpec.Equals(normalizedRequiredSpec, StringComparison.OrdinalIgnoreCase);
}
```

**? This is correct** - belongs in domain as it's business logic

---

### 2. Application Layer (WorkerService)

**File**: `Application/Services/WorkerService.cs`

#### ? Duplicate Specialization Logic:
```csharp
private string DetermineRequiredSpecialization(string description)
{
    var normalizedDescription = description.ToLowerInvariant();
    
    // Check each CONFIGURED specialization mapping
    foreach (var mapping in _specializationSettings.Mappings)
    {
        if (mapping.Keywords.Any(keyword =>
     normalizedDescription.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
        {
  return mapping.Specialization;
        }
    }
    
    return _specializationSettings.DefaultSpecialization;
}
```

**Problem**: Same logic as `Worker.DetermineRequiredSpecialization()` but:
- ? Uses configuration (good)
- ? Duplicates domain logic (bad)
- ? Two different implementations can diverge

---

### 3. Configuration Layer

**File**: `Application/Common/Configuration/SpecializationSettings.cs`

```csharp
public class SpecializationSettings
{
    public List<SpecializationMapping> Mappings { get; set; } = new();
    public string DefaultSpecialization { get; set; } = "General Maintenance";
}

public class SpecializationMapping
{
    public string Specialization { get; set; } = string.Empty;
    public List<string> Keywords { get; set; } = new();
}
```

**File**: `WebUI/appsettings.WorkerService.json`

```json
{
  "Specialization": {
    "DefaultSpecialization": "General Maintenance",
    "Mappings": [
      {
   "Specialization": "Plumber",
     "Keywords": ["plumb", "leak", "water", "toilet", "faucet", "pipe", "drain", "sink"]
      },
      {
        "Specialization": "Electrician",
        "Keywords": ["electric", "outlet", "light", "wire", "circuit", "power"]
      }
      // ... 5 more
    ]
  }
}
```

**? Good**:
- Configurable keyword mappings
- No code changes to add specializations
- Maintainable

**? Problem**:
- Configuration is in Application/WebUI layer
- Domain doesn't know about configuration
- Two systems: domain hard-coded + application configured

---

### 4. Domain Service

**File**: `Domain/Services/UnitSchedulingService.cs`

#### ? Third Copy of Normalization Logic:
```csharp
private static string NormalizeSpecialization(string specialization)
{
    return specialization.ToLowerInvariant() switch
    {
        "plumber" => "Plumbing",
        "electrician" => "Electrical",
        // ... SAME 12 hard-coded cases as Worker entity
      _ => specialization
    };
}
```

**Problem**: **Three copies of same logic!**
1. `Worker.NormalizeSpecialization()`
2. `UnitSchedulingService.NormalizeSpecialization()`
3. Implicitly in `WorkerService` via configuration

---

## Usage Analysis

### Where Specialization is Used:

| Location | Method | Purpose | Layer |
|----------|--------|---------|-------|
| **Worker.cs** | `Specialization` property | Store worker's skill | Domain ? |
| **Worker.cs** | `SetSpecialization()` | Change specialization | Domain ? |
| **Worker.cs** | `HasSpecializedSkills()` | Check if can do work | Domain ? |
| **Worker.cs** | `DetermineRequiredSpecialization()` | Infer from description | Domain ? |
| **Worker.cs** | `NormalizeSpecialization()` | Handle variations | Domain ? |
| **Worker.cs** | `ValidateCanBeAssignedToRequest()` | Validate assignment | Domain ? |
| **WorkerService** | `DetermineRequiredSpecialization()` | Infer from description | Application ? |
| **UnitSchedulingService** | `NormalizeSpecialization()` | Handle variations | Domain ? |
| **UnitSchedulingService** | `DoesSpecializationMatch()` | Check match | Domain ? |
| **GetAvailableWorkersQuery** | Filter by specialization | Query workers | Application ? |
| **ScheduleServiceWorkCommand** | Validate specialization | Assignment validation | Application ? |

---

## Problems Identified

### 1. ? Logic Duplication (DRY Violation)

**Three implementations of normalization**:
- `Worker.NormalizeSpecialization()`
- `UnitSchedulingService.NormalizeSpecialization()`
- Configuration-based in `WorkerService`

**Two implementations of determination**:
- `Worker.DetermineRequiredSpecialization()` (hard-coded)
- `WorkerService.DetermineRequiredSpecialization()` (configured)

### 2. ? Inconsistency Risk

Hard-coded domain logic can diverge from configured application logic:

```csharp
// Domain: Returns "Plumbing"
Worker.DetermineRequiredSpecialization("Leaking faucet", "Water dripping")

// Application: Could return "Plumber" if configured differently
_workerService.DetermineRequiredSpecialization("Leaking faucet water dripping")
```

### 3. ? Configuration Split

Specialization values appear in multiple places:
- **Hard-coded in Domain**: "Plumbing", "Electrical", "HVAC", etc.
- **Configured in Application**: Keyword mappings
- **Hard-coded in UI**: Registration dropdown (8 options)
- **Configured in appsettings**: JSON keyword lists

### 4. ? Testability Issues

Domain tests require knowing hard-coded keyword lists:

```csharp
[Theory]
[InlineData("Leaking faucet", "Plumbing")] // Must know "leak" ? Plumbing
[InlineData("Outlet sparking", "Electrical")] // Must know "outlet" ? Electrical
public void DetermineSpecialization_Tests(string desc, string expected)
{
    var result = Worker.DetermineRequiredSpecialization("Request", desc);
    result.Should().Be(expected);
}
```

**Problem**: Tests are brittle - break if keywords change

---

## Domain-Driven Design Analysis

### Question: Should Specialization be in Domain?

**YES** ? - Specialization is a **core business concept**:

1. **Identity**: Workers are defined by their specialization
2. **Business Rules**: 
   - Worker can only do work matching their specialization
   - "General Maintenance" is special (can do anything)
   - Specialization changes trigger events
3. **Invariants**:
   - Specialization cannot be empty once set
   - Worker must have specialization to be assigned
4. **Domain Events**: `WorkerSpecializationChangedEvent`

### What Should Be Where?

| Concept | Current Location | Should Be |
|---------|-----------------|-----------|
| **Specialization Value** | Domain ? | Domain ? |
| **SetSpecialization()** | Domain ? | Domain ? |
| **HasSpecializedSkills()** | Domain ? | Domain ? |
| **Specialization List** | Hard-coded everywhere ? | **Value Object** ? |
| **Normalization** | 3 copies ? | **Value Object** ? |
| **Keyword Determination** | Domain + Application ? | **Domain Service** ? |
| **Keyword Mappings** | appsettings.json ? | **Seed Data / Reference Data** ? |

---

## Recommended Solution

### ? Option 1: Specialization Value Object (Recommended)

Create a **Specialization Value Object** in the Domain layer:

```csharp
// Domain/ValueObjects/Specialization.cs
public sealed class Specialization : ValueObject
{
    // Standard specializations
    public static readonly Specialization Plumbing = new("Plumbing");
    public static readonly Specialization Electrical = new("Electrical");
    public static readonly Specialization HVAC = new("HVAC");
    public static readonly Specialization GeneralMaintenance = new("General Maintenance");
    public static readonly Specialization Carpentry = new("Carpentry");
    public static readonly Specialization Painting = new("Painting");
    public static readonly Specialization Locksmith = new("Locksmith");
    public static readonly Specialization ApplianceRepair = new("Appliance Repair");
    
    private static readonly Dictionary<string, Specialization> _knownSpecializations = new()
    {
        ["plumbing"] = Plumbing,
["plumber"] = Plumbing,
        ["electrical"] = Electrical,
      ["electrician"] = Electrical,
        ["hvac"] = HVAC,
        ["hvac technician"] = HVAC,
        ["general maintenance"] = GeneralMaintenance,
        ["maintenance"] = GeneralMaintenance,
   ["carpentry"] = Carpentry,
        ["carpenter"] = Carpentry,
        ["painting"] = Painting,
        ["painter"] = Painting,
        ["locksmith"] = Locksmith,
        ["appliance repair"] = ApplianceRepair,
        ["appliance technician"] = ApplianceRepair
    };
    
    public string Value { get; }
    
  private Specialization(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
throw new ArgumentException("Specialization cannot be empty");
        
        Value = value;
    }
    
    /// <summary>
    /// Parse specialization from string (handles variations)
    /// </summary>
    public static Specialization Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
         return GeneralMaintenance;
      
        var normalized = value.Trim().ToLowerInvariant();
     
        if (_knownSpecializations.TryGetValue(normalized, out var specialization))
    return specialization;
     
     // Allow custom specializations
        return new Specialization(value.Trim());
 }
    
    /// <summary>
    /// Check if this specialization can handle work requiring another specialization
/// </summary>
    public bool CanHandle(Specialization required)
  {
        if (required == null)
          return true;
    
        // Exact match
     if (Equals(required))
            return true;
      
        // General Maintenance can handle anything
        if (Equals(GeneralMaintenance))
     return true;
        
   return false;
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
    
    public override string ToString() => Value;
 
    public static implicit operator string(Specialization specialization) => specialization.Value;
    public static implicit operator Specialization(string value) => Parse(value);
}
```

**Update Worker entity**:
```csharp
public class Worker : BaseEntity, IAggregateRoot
{
    public Specialization? Specialization { get; private set; }
    
    public void SetSpecialization(Specialization specialization)
    {
        if (specialization == null)
  throw new ArgumentNullException(nameof(specialization));
        
        var oldSpec = Specialization;
        Specialization = specialization;
        
if (oldSpec != specialization)
   AddDomainEvent(new WorkerSpecializationChangedEvent(this, oldSpec?.Value, specialization.Value));
    }
    
    public bool HasSpecializedSkills(Specialization requiredSpecialization)
    {
        if (requiredSpecialization == null || Specialization == null)
            return true;
        
    return Specialization.CanHandle(requiredSpecialization);
    }
}
```

---

### ? Option 2: Specialization Determination Service

Create a **Domain Service** for specialization inference:

```csharp
// Domain/Services/SpecializationDeterminationService.cs
public class SpecializationDeterminationService
{
  private readonly Dictionary<string, List<string>> _specializationKeywords = new()
    {
     ["Plumbing"] = new() { "plumb", "leak", "water", "drain", "pipe", "faucet", "toilet", "sink" },
      ["Electrical"] = new() { "electric", "power", "outlet", "wiring", "light", "switch", "breaker" },
        ["HVAC"] = new() { "heat", "hvac", "air", "furnace", "thermostat", "cooling", "ventilation" },
 ["Locksmith"] = new() { "lock", "key", "security", "deadbolt" },
     ["Painting"] = new() { "paint", "wall", "ceiling", "trim", "brush", "roller" },
     ["Carpentry"] = new() { "wood", "cabinet", "door", "frame", "carpenter", "build" },
        ["Appliance Repair"] = new() { "appliance", "refrigerator", "washer", "dryer", "dishwasher", "oven" }
    };
    
    public Specialization DetermineFromDescription(string title, string description)
    {
 if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(description))
      return Specialization.GeneralMaintenance;
        
        string text = $"{title} {description}".ToLowerInvariant();
   
        foreach (var (specialization, keywords) in _specializationKeywords)
        {
  if (keywords.Any(keyword => text.Contains(keyword)))
         return Specialization.Parse(specialization);
        }
        
        return Specialization.GeneralMaintenance;
    }
}
```

**Benefits**:
- ? Single source of truth
- ? Domain logic in domain layer
- ? Easily testable
- ? Can be configured via database/seed data

---

### ? Option 3: Keep Configuration (Current Approach)

**Problems**:
- ? Business logic in configuration
- ? Domain doesn't own its own rules
- ? Inconsistency between layers
- ? Hard to test

---

## Migration Plan

### Phase 1: Create Value Object ?

1. Create `Specialization` value object
2. Update `Worker` entity to use it
3. Add tests for value object
4. Update database mappings (EF Core conversion)

### Phase 2: Create Domain Service ?

1. Create `SpecializationDeterminationService`
2. Move keyword logic from `Worker.DetermineRequiredSpecialization()` to service
3. Inject service where needed (command handlers)
4. Update tests

### Phase 3: Remove Duplication ?

1. Delete `Worker.DetermineRequiredSpecialization()` static method
2. Delete `Worker.NormalizeSpecialization()` private method
3. Delete `UnitSchedulingService.NormalizeSpecialization()` method
4. Delete `WorkerService.DetermineRequiredSpecialization()` method
5. Delete `SpecializationSettings` configuration

### Phase 4: Update Application Layer ?

1. Update `WorkerService` to use domain service
2. Update command handlers to use domain service
3. Update query handlers to use `Specialization` value object
4. Remove configuration from appsettings.json

### Phase 5: Update Tests ?

1. Update domain tests to use value object
2. Update application tests to use domain service
3. Remove configuration-based tests

---

## Benefits of Domain Approach

### ? 1. Single Source of Truth

All specialization logic in one place (Domain):
- Value object for values
- Domain service for determination
- No duplication across layers

### ? 2. Type Safety

```csharp
// Before (stringly-typed)
worker.SetSpecialization("Plumber"); // Typo? Runtime error!

// After (type-safe)
worker.SetSpecialization(Specialization.Plumbing); // Compile-time safe!
```

### ? 3. Consistency

Impossible to have different implementations:
- Domain and Application use same logic
- Normalization is automatic
- No configuration drift

### ? 4. Testability

```csharp
[Fact]
public void Specialization_Plumbing_CanHandle_Plumbing()
{
var plumber = Specialization.Plumbing;
    var required = Specialization.Plumbing;
    
    plumber.CanHandle(required).Should().BeTrue();
}

[Fact]
public void Specialization_GeneralMaintenance_CanHandleAnything()
{
    var general = Specialization.GeneralMaintenance;
    
    general.CanHandle(Specialization.Plumbing).Should().BeTrue();
    general.CanHandle(Specialization.Electrical).Should().BeTrue();
}
```

### ? 5. Extensibility

Easy to add new specializations:
```csharp
public static readonly Specialization Roofing = new("Roofing");
public static readonly Specialization Flooring = new("Flooring");
```

### ? 6. Domain Events

```csharp
public class WorkerSpecializationChangedEvent : DomainEvent
{
    public Worker Worker { get; }
  public Specialization? OldSpecialization { get; }
    public Specialization NewSpecialization { get; }
}
```

---

## Code Samples

### Before (Current):
```csharp
// Domain
public string? Specialization { get; private set; }
public static string DetermineRequiredSpecialization(string title, string description) { ... }
private static string NormalizeSpecialization(string specialization) { ... }

// Application
private string DetermineRequiredSpecialization(string description) { ... }

// Domain Service
private static string NormalizeSpecialization(string specialization) { ... }

// Configuration
"Specialization": { "Mappings": [...] }
```

### After (Recommended):
```csharp
// Domain Value Object
public sealed class Specialization : ValueObject
{
    public static readonly Specialization Plumbing = new("Plumbing");
    public string Value { get; }
    public bool CanHandle(Specialization required) { ... }
}

// Domain Service
public class SpecializationDeterminationService
{
public Specialization DetermineFromDescription(string title, string description) { ... }
}

// Domain Entity
public Specialization? Specialization { get; private set; }

// Application - just uses domain
var service = new SpecializationDeterminationService();
var spec = service.DetermineFromDescription(request.Title, request.Description);
```

---

## Summary

### Current State: ? **BAD**
- Logic split across 3 layers
- 3 copies of normalization
- 2 copies of determination
- Hard-coded + configured = inconsistency

### Recommended State: ? **GOOD**
- All logic in Domain layer
- Value Object for type safety
- Domain Service for determination
- Single source of truth
- Testable, maintainable, consistent

---

## Recommendation

**? YES - Move ALL Specialization Logic to Domain Layer**

**Action Items**:
1. Create `Specialization` value object (Priority: HIGH)
2. Create `SpecializationDeterminationService` (Priority: HIGH)
3. Update `Worker` entity to use value object (Priority: HIGH)
4. Delete duplicate normalization methods (Priority: MEDIUM)
5. Delete `WorkerService.DetermineRequiredSpecialization()` (Priority: MEDIUM)
6. Delete `SpecializationSettings` configuration (Priority: LOW)
7. Update all tests (Priority: HIGH)

**Effort**: ~4-6 hours  
**Risk**: Low (well-tested domain concept)  
**Benefit**: High (cleaner architecture, type safety, consistency)

---

**Verdict**: ? **REFACTOR TO DOMAIN LAYER**
