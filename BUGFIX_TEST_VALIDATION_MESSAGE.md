# Bug Fix: TenantRequestTests.Schedule_ShouldThrowException_WithInvalidScheduleDate

## Issue

Test was failing: `RentalRepairs.Domain.Tests.Entities.TenantRequestTests.Schedule_ShouldThrowException_WithInvalidScheduleDate(dateString: "2023-01-01")`

## Root Cause

The test expectation was outdated and didn't match the current validation message in the domain entity.

**Test Expected:**
```csharp
.WithMessage("*must be in the future*");
```

**Actual Domain Validation:**
```csharp
if (scheduledDate.Date < DateTime.Today)
{
    throw new TenantRequestDomainException("Scheduled date must be today or in the future");
}
```

## Background

The validation was updated in **Phase 3** to allow scheduling for **today OR future dates** (not just future dates). This was part of the UTC date fix that changed the date comparison from `DateTime.UtcNow` to `DateTime.Today` to allow scheduling work for the current day.

The test was not updated to reflect this business rule change.

## Solution

Updated the test expectation to match the current validation message:

```csharp
[Theory]
[InlineData("2023-01-01")] // Past date
public void Schedule_ShouldThrowException_WithInvalidScheduleDate(string dateString)
{
  // Arrange
    var request = CreateTestTenantRequest();
    request.Submit();
 
    var invalidDate = DateTime.Parse(dateString);

    // Act & Assert
 Action act = () => request.Schedule(invalidDate, "worker@test.com", "WO-123");
    act.Should().Throw<TenantRequestDomainException>()
     .WithMessage("*must be today or in the future*"); // ? FIXED
}
```

## Validation

- ? Build successful
- ? Test now correctly validates that scheduling for past dates throws exception
- ? Error message matches actual domain business rule
- ? Consistent with Phase 3 UTC date fix

## Related Files

- `Domain\Entities\TenantRequest.cs` - Contains the validation logic (line ~350)
- `Domain.Tests\Entities\TenantRequestTests.cs` - Test file that was fixed
- `UTC_DATE_FIX.md` - Original Phase 3 fix that changed the validation rule

## Business Rule Confirmed

? **Scheduled date must be today or in the future** (not past dates)
- Past dates (before today): ? Throw exception
- Today: ? Allowed
- Future dates: ? Allowed

This aligns with the real-world business need to schedule maintenance work for the current day.
