using Xunit;
using RentalRepairs.Domain.Services;
using FluentAssertions;

namespace RentalRepairs.Domain.Tests.Services;

/// <summary>
/// Comprehensive tests for simplified UnitSchedulingService business rules
/// Tests: specialization match, no double booking, unit exclusivity, max 2 per unit, emergency overrides
/// </summary>
public class UnitSchedulingServiceTests
{
    private readonly UnitSchedulingService _service;
    private readonly DateTime _testDate = new DateTime(2025, 1, 15);
    private readonly Guid _testRequestId = Guid.NewGuid();

    public UnitSchedulingServiceTests()
    {
        _service = new UnitSchedulingService();
    }

    #region Rule 1: Specialization Match Tests

    [Fact]
    public void ValidateWorkerAssignment_MatchingSpecialization_ShouldBeValid()
    {
        // Arrange
        var existingAssignments = new List<ExistingAssignment>();

        // Act
        var result = _service.ValidateWorkerAssignment(
            _testRequestId, "PROP001", "101", _testDate, "plumber@test.com", "Plumbing", "Plumbing", false, existingAssignments);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ConflictType.Should().Be(SchedulingConflictType.None);
    }

    [Fact]
    public void ValidateWorkerAssignment_GeneralMaintenanceWorker_ShouldHandleAnyWork()
    {
        // Arrange
        var existingAssignments = new List<ExistingAssignment>();

        // Act
        var result = _service.ValidateWorkerAssignment(
            _testRequestId, "PROP001", "101", _testDate, "general@test.com", "General Maintenance", "Plumbing", false, existingAssignments);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateWorkerAssignment_MismatchedSpecialization_ShouldBeInvalid()
    {
        // Arrange
        var existingAssignments = new List<ExistingAssignment>();

        // Act
        var result = _service.ValidateWorkerAssignment(
            _testRequestId, "PROP001", "101", _testDate, "hvac@test.com", "HVAC", "Plumbing", false, existingAssignments);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("HVAC cannot handle Plumbing work");
        result.ConflictType.Should().Be(SchedulingConflictType.SpecializationMismatch);
    }

    [Fact]
    public void ValidateWorkerAssignment_NoRequiredSpecialization_ShouldAlwaysBeValid()
    {
        // Arrange
        var existingAssignments = new List<ExistingAssignment>();

        // Act
        var result = _service.ValidateWorkerAssignment(
            _testRequestId, "PROP001", "101", _testDate, "hvac@test.com", "HVAC", "", false, existingAssignments);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Electrician", "Electrical")] // This was the bug - Electrician worker should handle Electrical work
    [InlineData("Electrical", "Electrician")] // Reverse case
    [InlineData("Plumber", "Plumbing")]
    [InlineData("Plumbing", "Plumber")]
    [InlineData("HVAC", "HVAC Technician")]
    [InlineData("Heating", "HVAC")]
    [InlineData("Cooling", "HVAC")]
    [InlineData("Painter", "Painting")]
    [InlineData("Carpenter", "Carpentry")]
    [InlineData("Appliance Technician", "Appliance Repair")]
    [InlineData("Maintenance", "General Maintenance")]
    public void ValidateWorkerAssignment_SpecializationNormalization_ShouldMatch(string workerSpecialization, string requiredSpecialization)
    {
        // Arrange
        var existingAssignments = new List<ExistingAssignment>();

        // Act
        var result = _service.ValidateWorkerAssignment(
            _testRequestId, "PROP001", "101", _testDate, "worker@test.com", workerSpecialization, requiredSpecialization, false, existingAssignments);

        // Assert
        result.IsValid.Should().BeTrue($"Worker specialized in '{workerSpecialization}' should be able to handle '{requiredSpecialization}' work");
        result.ConflictType.Should().Be(SchedulingConflictType.None);
    }

    [Fact]
    public void ValidateWorkerAssignment_ElectricianWorkerElectricalWork_ShouldBeValid()
    {
        // Arrange - This is the specific case from the error log
        var existingAssignments = new List<ExistingAssignment>();

        // Act
        var result = _service.ValidateWorkerAssignment(
            _testRequestId, "PROP001", "101", _testDate, "electrician.doe@demo.com", "Electrician", "Electrical", false, existingAssignments);

        // Assert
        result.IsValid.Should().BeTrue("An Electrician should be able to handle Electrical work");
        result.ConflictType.Should().Be(SchedulingConflictType.None);
        result.ErrorMessage.Should().BeEmpty();
    }

    #endregion

    #region Rule 2: No Double Booking Tests

    [Fact]
    public void ValidateWorkerAssignment_WorkerBookedElsewhere_ShouldBeInvalid()
    {
        // Arrange
        var existingAssignments = new List<ExistingAssignment>
        {
            new ExistingAssignment
            {
                TenantRequestId = Guid.NewGuid(),
                PropertyCode = "PROP001",
                UnitNumber = "102", // Different unit
                WorkerEmail = "plumber@test.com",
                ScheduledDate = _testDate,
                Status = "Scheduled"
            }
        };

        // Act
        var result = _service.ValidateWorkerAssignment(
            _testRequestId, "PROP001", "101", _testDate, "plumber@test.com", "Plumbing", "Plumbing", false, existingAssignments);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already assigned to PROP001 Unit 102");
        result.ConflictType.Should().Be(SchedulingConflictType.WorkerDoubleBooked);
    }

    [Fact]
    public void ValidateWorkerAssignment_WorkerBookedDifferentProperty_ShouldBeInvalid()
    {
        // Arrange
        var existingAssignments = new List<ExistingAssignment>
        {
            new ExistingAssignment
            {
                PropertyCode = "PROP002", // Different property
                UnitNumber = "101",
                WorkerEmail = "plumber@test.com",
                ScheduledDate = _testDate,
                Status = "Scheduled"
            }
        };

        // Act
        var result = _service.ValidateWorkerAssignment(
            _testRequestId, "PROP001", "101", _testDate, "plumber@test.com", "Plumbing", "Plumbing", false, existingAssignments);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ConflictType.Should().Be(SchedulingConflictType.WorkerDoubleBooked);
    }

    [Fact]
    public void ValidateWorkerAssignment_EmergencyWorkerBookedElsewhere_ShouldStillBeInvalid()
    {
        // Arrange
        var existingAssignments = new List<ExistingAssignment>
        {
            new ExistingAssignment
            {
                PropertyCode = "PROP001",
                UnitNumber = "102",
                WorkerEmail = "plumber@test.com",
                ScheduledDate = _testDate,
                Status = "Scheduled"
            }
        };

        // Act
        var result = _service.ValidateWorkerAssignment(
            _testRequestId, "PROP001", "101", _testDate, "plumber@test.com", "Plumbing", "Plumbing", true, existingAssignments);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("cannot override worker");
        result.ErrorMessage.Should().Contain("being physically assigned elsewhere");
        result.ConflictType.Should().Be(SchedulingConflictType.WorkerDoubleBooked);
    }

    #endregion

    #region Rule 3: Unit Exclusivity Tests

    [Fact]
    public void ValidateWorkerAssignment_OtherWorkerInUnit_ShouldBeInvalid()
    {
        // Arrange
        var existingAssignments = new List<ExistingAssignment>
        {
            new ExistingAssignment
            {
                PropertyCode = "PROP001",
                UnitNumber = "101", // Same unit
                WorkerEmail = "hvac@test.com", // Different worker
                ScheduledDate = _testDate,
                Status = "Scheduled"
            }
        };

        // Act
        var result = _service.ValidateWorkerAssignment(
            _testRequestId, "PROP001", "101", _testDate, "plumber@test.com", "Plumbing", "Plumbing", false, existingAssignments);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Unit 101 already has worker hvac@test.com assigned");
        result.ConflictType.Should().Be(SchedulingConflictType.UnitConflict);
    }

    [Fact]
    public void ValidateWorkerAssignment_EmergencyRevokesNormalInUnit_ShouldBeValid()
    {
        // Arrange
        var existingAssignments = new List<ExistingAssignment>
        {
            new ExistingAssignment
            {
                TenantRequestId = Guid.NewGuid(),
                PropertyCode = "PROP001",
                UnitNumber = "101",
                WorkerEmail = "hvac@test.com",
                ScheduledDate = _testDate,
                Status = "Scheduled",
                IsEmergency = false // Normal request
            }
        };

        // Act
        var result = _service.ValidateWorkerAssignment(
            _testRequestId, "PROP001", "101", _testDate, "plumber@test.com", "Plumbing", "Plumbing", true, existingAssignments);

        // Assert
        result.IsValid.Should().BeTrue();
        result.AssignmentsToCancelForEmergency.Should().HaveCount(1);
        result.AssignmentsToCancelForEmergency.First().WorkerEmail.Should().Be("hvac@test.com");
    }

    [Fact]
    public void ValidateWorkerAssignment_EmergencyConflictsWithEmergency_ShouldNotifyButAllow()
    {
        // Arrange
        var existingAssignments = new List<ExistingAssignment>
        {
            new ExistingAssignment
            {
                TenantRequestId = Guid.NewGuid(),
                PropertyCode = "PROP001",
                UnitNumber = "101",
                WorkerEmail = "hvac@test.com",
                ScheduledDate = _testDate,
                Status = "Scheduled",
                IsEmergency = true // Emergency request
            }
        };

        // Act
        var result = _service.ValidateWorkerAssignment(
            _testRequestId, "PROP001", "101", _testDate, "plumber@test.com", "Plumbing", "Plumbing", true, existingAssignments);

        // Assert
        result.IsValid.Should().BeTrue();
        result.HasEmergencyConflicts.Should().BeTrue();
        result.EmergencyConflicts.Should().HaveCount(1);
        result.EmergencyConflicts.First().WorkerEmail.Should().Be("hvac@test.com");
        result.AssignmentsToCancelForEmergency.Should().BeEmpty(); // Don't cancel emergency requests
    }

    #endregion

    #region Rule 4: Max 2 Requests Per Worker Per Unit Tests

    [Fact]
    public void ValidateWorkerAssignment_WorkerHasOneRequestInUnit_ShouldBeValid()
    {
        // Arrange
        var existingAssignments = new List<ExistingAssignment>
        {
            new ExistingAssignment
            {
                TenantRequestId = Guid.NewGuid(),
                PropertyCode = "PROP001",
                UnitNumber = "101",
                WorkerEmail = "plumber@test.com",
                ScheduledDate = _testDate,
                Status = "Scheduled"
            }
        };

        // Act
        var result = _service.ValidateWorkerAssignment(
            _testRequestId, "PROP001", "101", _testDate, "plumber@test.com", "Plumbing", "Plumbing", false, existingAssignments);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateWorkerAssignment_WorkerHasTwoRequestsInUnit_ShouldBeInvalid()
    {
        // Arrange
        var existingAssignments = new List<ExistingAssignment>
        {
            new ExistingAssignment
            {
                TenantRequestId = Guid.NewGuid(),
                PropertyCode = "PROP001",
                UnitNumber = "101",
                WorkerEmail = "plumber@test.com",
                ScheduledDate = _testDate,
                Status = "Scheduled"
            },
            new ExistingAssignment
            {
                TenantRequestId = Guid.NewGuid(),
                PropertyCode = "PROP001",
                UnitNumber = "101",
                WorkerEmail = "plumber@test.com",
                ScheduledDate = _testDate,
                Status = "Scheduled"
            }
        };

        // Act
        var result = _service.ValidateWorkerAssignment(
            _testRequestId, "PROP001", "101", _testDate, "plumber@test.com", "Plumbing", "Plumbing", false, existingAssignments);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already has maximum 2 assignments in Unit 101");
        result.ConflictType.Should().Be(SchedulingConflictType.WorkerUnitLimit);
    }

    [Fact]
    public void ValidateWorkerAssignment_EmergencyRevokesNormalFromSameWorker_ShouldBeValid()
    {
        // Arrange
        var existingAssignments = new List<ExistingAssignment>
        {
            new ExistingAssignment
            {
                TenantRequestId = Guid.NewGuid(),
                PropertyCode = "PROP001",
                UnitNumber = "101",
                WorkerEmail = "plumber@test.com",
                ScheduledDate = _testDate,
                Status = "Scheduled",
                IsEmergency = false
            },
            new ExistingAssignment
            {
                TenantRequestId = Guid.NewGuid(),
                PropertyCode = "PROP001",
                UnitNumber = "101",
                WorkerEmail = "plumber@test.com",
                ScheduledDate = _testDate,
                Status = "Scheduled",
                IsEmergency = false
            }
        };

        // Act
        var result = _service.ValidateWorkerAssignment(
            _testRequestId, "PROP001", "101", _testDate, "plumber@test.com", "Plumbing", "Plumbing", true, existingAssignments);

        // Assert
        result.IsValid.Should().BeTrue();
        result.AssignmentsToCancelForEmergency.Should().HaveCount(2);
    }

    [Fact]
    public void ValidateWorkerAssignment_EmergencyWithTwoEmergenciesInUnit_ShouldNotifyConflict()
    {
        // Arrange
        var existingAssignments = new List<ExistingAssignment>
        {
            new ExistingAssignment
            {
                TenantRequestId = Guid.NewGuid(),
                PropertyCode = "PROP001",
                UnitNumber = "101",
                WorkerEmail = "plumber@test.com",
                ScheduledDate = _testDate,
                Status = "Scheduled",
                IsEmergency = true
            },
            new ExistingAssignment
            {
                TenantRequestId = Guid.NewGuid(),
                PropertyCode = "PROP001",
                UnitNumber = "101",
                WorkerEmail = "plumber@test.com",
                ScheduledDate = _testDate,
                Status = "Scheduled",
                IsEmergency = true
            }
        };

        // Act
        var result = _service.ValidateWorkerAssignment(
            _testRequestId, "PROP001", "101", _testDate, "plumber@test.com", "Plumbing", "Plumbing", true, existingAssignments);

        // Assert
        result.IsValid.Should().BeTrue();
        result.HasEmergencyConflicts.Should().BeTrue();
        result.EmergencyConflicts.Should().HaveCount(2);
    }

    #endregion

    #region Status Filtering Tests

    [Fact]
    public void ValidateWorkerAssignment_CompletedAssignmentsIgnored_ShouldBeValid()
    {
        // Arrange
        var existingAssignments = new List<ExistingAssignment>
        {
            new ExistingAssignment
            {
                PropertyCode = "PROP001",
                UnitNumber = "101",
                WorkerEmail = "hvac@test.com",
                ScheduledDate = _testDate,
                Status = "Completed" // Should be ignored
            }
        };

        // Act
        var result = _service.ValidateWorkerAssignment(
            _testRequestId, "PROP001", "101", _testDate, "plumber@test.com", "Plumbing", "Plumbing", false, existingAssignments);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateWorkerAssignment_InProgressAssignmentsConsidered_ShouldBeInvalid()
    {
        // Arrange
        var existingAssignments = new List<ExistingAssignment>
        {
            new ExistingAssignment
            {
                PropertyCode = "PROP001",
                UnitNumber = "101",
                WorkerEmail = "hvac@test.com",
                ScheduledDate = _testDate,
                Status = "InProgress" // Should be considered
            }
        };

        // Act
        var result = _service.ValidateWorkerAssignment(
            _testRequestId, "PROP001", "101", _testDate, "plumber@test.com", "Plumbing", "Plumbing", false, existingAssignments);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ConflictType.Should().Be(SchedulingConflictType.UnitConflict);
    }

    #endregion

    #region Date Filtering Tests

    [Fact]
    public void ValidateWorkerAssignment_DifferentDate_ShouldBeValid()
    {
        // Arrange
        var existingAssignments = new List<ExistingAssignment>
        {
            new ExistingAssignment
            {
                PropertyCode = "PROP001",
                UnitNumber = "101",
                WorkerEmail = "hvac@test.com",
                ScheduledDate = _testDate.AddDays(1), // Different date
                Status = "Scheduled"
            }
        };

        // Act
        var result = _service.ValidateWorkerAssignment(
            _testRequestId, "PROP001", "101", _testDate, "plumber@test.com", "Plumbing", "Plumbing", false, existingAssignments);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Emergency Override Processing Tests

    [Fact]
    public void ProcessEmergencyOverride_WithAssignments_ShouldReturnCancelledInfo()
    {
        // Arrange
        var requestId1 = Guid.NewGuid();
        var requestId2 = Guid.NewGuid();
        var assignmentsToCancel = new List<ExistingAssignment>
        {
            new ExistingAssignment
            {
                TenantRequestId = requestId1,
                WorkerEmail = "worker1@test.com",
                WorkOrderNumber = "WO-001",
                ScheduledDate = _testDate
            },
            new ExistingAssignment
            {
                TenantRequestId = requestId2,
                WorkerEmail = "worker2@test.com",
                WorkOrderNumber = "WO-002",
                ScheduledDate = _testDate
            }
        };

        // Act
        var result = _service.ProcessEmergencyOverride(assignmentsToCancel);

        // Assert
        result.CancelledRequestIds.Should().HaveCount(2);
        result.CancelledRequestIds.Should().Contain(requestId1);
        result.CancelledRequestIds.Should().Contain(requestId2);
        
        result.CancelledAssignments.Should().HaveCount(2);
        result.CancelledAssignments.All(a => a.CancellationReason.Contains("emergency")).Should().BeTrue();
    }

    #endregion

    #region Complex Integration Tests

    [Fact]
    public void ValidateWorkerAssignment_ComplexEmergencyScenario_ShouldHandleCorrectly()
    {
        // Arrange: Complex scenario
        var normalRequest1 = Guid.NewGuid();
        var normalRequest2 = Guid.NewGuid(); 
        var emergencyRequest1 = Guid.NewGuid();
        
        var existingAssignments = new List<ExistingAssignment>
        {
            // Same unit: 1 normal, 1 emergency (different workers)
            new ExistingAssignment
            {
                TenantRequestId = normalRequest1,
                PropertyCode = "PROP001",
                UnitNumber = "101",
                WorkerEmail = "hvac@test.com",
                ScheduledDate = _testDate,
                Status = "Scheduled",
                IsEmergency = false
            },
            new ExistingAssignment
            {
                TenantRequestId = emergencyRequest1,
                PropertyCode = "PROP001",
                UnitNumber = "101",
                WorkerEmail = "electrician@test.com",
                ScheduledDate = _testDate,
                Status = "Scheduled",
                IsEmergency = true
            },
            // Same worker: 1 assignment in different unit (should block)
            new ExistingAssignment
            {
                TenantRequestId = normalRequest2,
                PropertyCode = "PROP001",
                UnitNumber = "102",
                WorkerEmail = "plumber@test.com",
                ScheduledDate = _testDate,
                Status = "Scheduled",
                IsEmergency = false
            }
        };

        // Act: Try to assign plumber to emergency in unit 101
        var result = _service.ValidateWorkerAssignment(
            _testRequestId, "PROP001", "101", _testDate, "plumber@test.com", "Plumbing", "Plumbing", true, existingAssignments);

        // Assert: Should fail due to worker being booked elsewhere
        result.IsValid.Should().BeFalse();
        result.ConflictType.Should().Be(SchedulingConflictType.WorkerDoubleBooked);
        result.ErrorMessage.Should().Contain("assigned elsewhere");
    }

    #endregion
}