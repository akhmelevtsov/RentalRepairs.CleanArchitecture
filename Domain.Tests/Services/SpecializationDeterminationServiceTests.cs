using FluentAssertions;
using RentalRepairs.Domain.Enums;
using RentalRepairs.Domain.Services;
using Xunit;

namespace RentalRepairs.Domain.Tests.Services;

/// <summary>
/// Tests for SpecializationDeterminationService domain service.
/// Validates keyword matching, priority ordering, and business rules.
/// </summary>
public class SpecializationDeterminationServiceTests
{
    private readonly SpecializationDeterminationService _service;

    public SpecializationDeterminationServiceTests()
    {
        _service = new SpecializationDeterminationService();
    }

    #region DetermineRequiredSpecialization Tests

    [Theory]
    [InlineData("Leaking faucet", "Water dripping under sink", WorkerSpecialization.Plumbing)]
    [InlineData("Toilet won't flush", "Bathroom toilet issue", WorkerSpecialization.Plumbing)]
    [InlineData("Pipe burst", "Water pipe broken in kitchen", WorkerSpecialization.Plumbing)]
    [InlineData("Drain clogged", "Sink drain backing up", WorkerSpecialization.Plumbing)]
    public void DetermineRequiredSpecialization_ShouldReturnPlumbing_ForWaterIssues(
        string title, string description, WorkerSpecialization expected)
    {
        // Act
        var result = _service.DetermineRequiredSpecialization(title, description);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Outlet sparking", "Power outlet in bedroom", WorkerSpecialization.Electrical)]
    [InlineData("Light not working", "Ceiling light fixture broken", WorkerSpecialization.Electrical)]
    [InlineData("Circuit breaker tripped", "Electrical panel issue", WorkerSpecialization.Electrical)]
    [InlineData("No power", "Wiring problem in living room", WorkerSpecialization.Electrical)]
    public void DetermineRequiredSpecialization_ShouldReturnElectrical_ForPowerIssues(
        string title, string description, WorkerSpecialization expected)
    {
        // Act
        var result = _service.DetermineRequiredSpecialization(title, description);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Heater not working", "Furnace won't turn on", WorkerSpecialization.HVAC)]
    [InlineData("AC broken", "Air conditioning not cooling", WorkerSpecialization.HVAC)]
    [InlineData("Too cold", "Thermostat issue, temperature problem", WorkerSpecialization.HVAC)]
    [InlineData("Ventilation problem", "HVAC system needs attention", WorkerSpecialization.HVAC)]
    [InlineData("Heating system broken", "Need HVAC tech", WorkerSpecialization.HVAC)]
    public void DetermineRequiredSpecialization_ShouldReturnHVAC_ForTemperatureIssues(
        string title, string description, WorkerSpecialization expected)
    {
        // Act
        var result = _service.DetermineRequiredSpecialization(title, description);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Locked out", "Lost my key, can't get in", WorkerSpecialization.Locksmith)]
    [InlineData("Deadbolt broken", "Lock on front door not working", WorkerSpecialization.Locksmith)]
    [InlineData("Need rekey", "Security issue with locks", WorkerSpecialization.Locksmith)]
    public void DetermineRequiredSpecialization_ShouldReturnLocksmith_ForLockIssues(
        string title, string description, WorkerSpecialization expected)
    {
        // Act
        var result = _service.DetermineRequiredSpecialization(title, description);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Paint peeling", "Need repainting work", WorkerSpecialization.Painting)]
    [InlineData("Color change", "Want to repaint bedroom with new color", WorkerSpecialization.Painting)]
    [InlineData("Painting needed", "Roller and brush work required", WorkerSpecialization.Painting)]
    public void DetermineRequiredSpecialization_ShouldReturnPainting_ForPaintIssues(
        string title, string description, WorkerSpecialization expected)
    {
        // Act
        var result = _service.DetermineRequiredSpecialization(title, description);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Cabinet door broken", "Kitchen wooden cabinet needs fixing", WorkerSpecialization.Carpentry)]
    [InlineData("Wooden shelf", "Need carpenter to build and install shelves", WorkerSpecialization.Carpentry)]
    [InlineData("Wood repair", "Carpenter needed for wood repair", WorkerSpecialization.Carpentry)]
    public void DetermineRequiredSpecialization_ShouldReturnCarpentry_ForWoodworkIssues(
        string title, string description, WorkerSpecialization expected)
    {
        // Act
        var result = _service.DetermineRequiredSpecialization(title, description);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Refrigerator broken", "Fridge not cooling", WorkerSpecialization.ApplianceRepair)]
    [InlineData("Washer won't spin", "Washing machine issue", WorkerSpecialization.ApplianceRepair)]
    [InlineData("Oven not heating", "Stove appliance problem", WorkerSpecialization.ApplianceRepair)]
    [InlineData("Dishwasher leaking", "Appliance repair needed", WorkerSpecialization.ApplianceRepair)]
    public void DetermineRequiredSpecialization_ShouldReturnApplianceRepair_ForApplianceIssues(
        string title, string description, WorkerSpecialization expected)
    {
        // Act
        var result = _service.DetermineRequiredSpecialization(title, description);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("General issue", "Something needs fixing", WorkerSpecialization.GeneralMaintenance)]
    [InlineData("Misc repair", "General maintenance needed", WorkerSpecialization.GeneralMaintenance)]
    [InlineData("", "", WorkerSpecialization.GeneralMaintenance)]
    [InlineData(null, null, WorkerSpecialization.GeneralMaintenance)]
    [InlineData("Unknown problem", "Not sure what's wrong", WorkerSpecialization.GeneralMaintenance)]
    public void DetermineRequiredSpecialization_ShouldReturnGeneralMaintenance_ForUnknownIssues(
        string title, string description, WorkerSpecialization expected)
    {
        // Act
        var result = _service.DetermineRequiredSpecialization(title, description);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void DetermineRequiredSpecialization_ShouldPrioritizeLocksmith_OverCarpentry()
    {
        // Arrange - "door" keyword exists in both Locksmith and Carpentry
        // but "lock" is more specific and checked first
        var title = "Door lock broken";
        var description = "The door lock needs to be fixed";

        // Act
        var result = _service.DetermineRequiredSpecialization(title, description);

        // Assert
        result.Should().Be(WorkerSpecialization.Locksmith,
            "Locksmith specialization should be prioritized over Carpentry for lock-related issues");
    }

    [Fact]
    public void DetermineRequiredSpecialization_ShouldBeCaseInsensitive()
    {
        // Act
        var result1 = _service.DetermineRequiredSpecialization("LEAKING FAUCET", "WATER DRIPPING");
        var result2 = _service.DetermineRequiredSpecialization("leaking faucet", "water dripping");
        var result3 = _service.DetermineRequiredSpecialization("Leaking Faucet", "Water Dripping");

        // Assert
        result1.Should().Be(WorkerSpecialization.Plumbing);
        result2.Should().Be(WorkerSpecialization.Plumbing);
        result3.Should().Be(WorkerSpecialization.Plumbing);
    }

    #endregion

    #region CanHandleWork Tests

    [Fact]
    public void CanHandleWork_ShouldReturnTrue_ForExactMatch()
    {
        // Act & Assert
        _service.CanHandleWork(WorkerSpecialization.Plumbing, WorkerSpecialization.Plumbing)
            .Should().BeTrue();

        _service.CanHandleWork(WorkerSpecialization.Electrical, WorkerSpecialization.Electrical)
            .Should().BeTrue();
    }

    [Fact]
    public void CanHandleWork_GeneralMaintenance_ShouldHandleAnyWork()
    {
        // Act & Assert
        _service.CanHandleWork(WorkerSpecialization.GeneralMaintenance, WorkerSpecialization.Plumbing)
            .Should().BeTrue();

        _service.CanHandleWork(WorkerSpecialization.GeneralMaintenance, WorkerSpecialization.Electrical)
            .Should().BeTrue();

        _service.CanHandleWork(WorkerSpecialization.GeneralMaintenance, WorkerSpecialization.HVAC)
            .Should().BeTrue();

        _service.CanHandleWork(WorkerSpecialization.GeneralMaintenance, WorkerSpecialization.Carpentry)
            .Should().BeTrue();

        _service.CanHandleWork(WorkerSpecialization.GeneralMaintenance, WorkerSpecialization.Painting)
            .Should().BeTrue();

        _service.CanHandleWork(WorkerSpecialization.GeneralMaintenance, WorkerSpecialization.Locksmith)
            .Should().BeTrue();

        _service.CanHandleWork(WorkerSpecialization.GeneralMaintenance, WorkerSpecialization.ApplianceRepair)
            .Should().BeTrue();
    }

    [Fact]
    public void CanHandleWork_Plumber_ShouldNotHandleElectricalWork()
    {
        // Act & Assert
        _service.CanHandleWork(WorkerSpecialization.Plumbing, WorkerSpecialization.Electrical)
            .Should().BeFalse();
    }

    [Fact]
    public void CanHandleWork_Electrician_ShouldNotHandlePlumbingWork()
    {
        // Act & Assert
        _service.CanHandleWork(WorkerSpecialization.Electrical, WorkerSpecialization.Plumbing)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(WorkerSpecialization.Plumbing, WorkerSpecialization.HVAC, false)]
    [InlineData(WorkerSpecialization.Electrical, WorkerSpecialization.Carpentry, false)]
    [InlineData(WorkerSpecialization.HVAC, WorkerSpecialization.Painting, false)]
    [InlineData(WorkerSpecialization.Carpentry, WorkerSpecialization.Locksmith, false)]
    [InlineData(WorkerSpecialization.Painting, WorkerSpecialization.ApplianceRepair, false)]
    public void CanHandleWork_DifferentSpecializations_ShouldReturnFalse(
        WorkerSpecialization workerSpec, WorkerSpecialization requiredSpec, bool expected)
    {
        // Act & Assert
        _service.CanHandleWork(workerSpec, requiredSpec).Should().Be(expected);
    }

    #endregion

    #region ParseSpecialization Tests

    [Theory]
    [InlineData("Plumbing", WorkerSpecialization.Plumbing)]
    [InlineData("plumbing", WorkerSpecialization.Plumbing)]
    [InlineData("Plumber", WorkerSpecialization.Plumbing)]
    [InlineData("plumber", WorkerSpecialization.Plumbing)]
    public void ParseSpecialization_ShouldHandlePlumbingVariations(string input, WorkerSpecialization expected)
    {
        // Act
        var result = _service.ParseSpecialization(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Electrical", WorkerSpecialization.Electrical)]
    [InlineData("electrical", WorkerSpecialization.Electrical)]
    [InlineData("Electrician", WorkerSpecialization.Electrical)]
    [InlineData("electrician", WorkerSpecialization.Electrical)]
    public void ParseSpecialization_ShouldHandleElectricalVariations(string input, WorkerSpecialization expected)
    {
        // Act
        var result = _service.ParseSpecialization(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("HVAC", WorkerSpecialization.HVAC)]
    [InlineData("hvac", WorkerSpecialization.HVAC)]
    [InlineData("HVAC Technician", WorkerSpecialization.HVAC)]
    [InlineData("Heating", WorkerSpecialization.HVAC)]
    [InlineData("Cooling", WorkerSpecialization.HVAC)]
    public void ParseSpecialization_ShouldHandleHVACVariations(string input, WorkerSpecialization expected)
    {
        // Act
        var result = _service.ParseSpecialization(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("General Maintenance", WorkerSpecialization.GeneralMaintenance)]
    [InlineData("general maintenance", WorkerSpecialization.GeneralMaintenance)]
    [InlineData("Maintenance", WorkerSpecialization.GeneralMaintenance)]
    [InlineData("General", WorkerSpecialization.GeneralMaintenance)]
    [InlineData("", WorkerSpecialization.GeneralMaintenance)]
    [InlineData(null, WorkerSpecialization.GeneralMaintenance)]
    [InlineData("   ", WorkerSpecialization.GeneralMaintenance)]
    public void ParseSpecialization_ShouldHandleGeneralMaintenanceVariations(string input,
        WorkerSpecialization expected)
    {
        // Act
        var result = _service.ParseSpecialization(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Carpentry", WorkerSpecialization.Carpentry)]
    [InlineData("Carpenter", WorkerSpecialization.Carpentry)]
    [InlineData("Painting", WorkerSpecialization.Painting)]
    [InlineData("Painter", WorkerSpecialization.Painting)]
    [InlineData("Locksmith", WorkerSpecialization.Locksmith)]
    [InlineData("Appliance Repair", WorkerSpecialization.ApplianceRepair)]
    [InlineData("Appliance Technician", WorkerSpecialization.ApplianceRepair)]
    public void ParseSpecialization_ShouldHandleOtherSpecializations(string input, WorkerSpecialization expected)
    {
        // Act
        var result = _service.ParseSpecialization(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ParseSpecialization_ShouldReturnGeneralMaintenance_ForUnknownInput()
    {
        // Act
        var result = _service.ParseSpecialization("UnknownSpecialization");

        // Assert
        result.Should().Be(WorkerSpecialization.GeneralMaintenance);
    }

    #endregion

    #region GetDisplayName Tests

    [Theory]
    [InlineData(WorkerSpecialization.GeneralMaintenance, "General Maintenance")]
    [InlineData(WorkerSpecialization.Plumbing, "Plumbing")]
    [InlineData(WorkerSpecialization.Electrical, "Electrical")]
    [InlineData(WorkerSpecialization.HVAC, "HVAC")]
    [InlineData(WorkerSpecialization.Carpentry, "Carpentry")]
    [InlineData(WorkerSpecialization.Painting, "Painting")]
    [InlineData(WorkerSpecialization.Locksmith, "Locksmith")]
    [InlineData(WorkerSpecialization.ApplianceRepair, "Appliance Repair")]
    public void GetDisplayName_ShouldReturnCorrectName(WorkerSpecialization specialization, string expected)
    {
        // Act
        var result = _service.GetDisplayName(specialization);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region GetAllSpecializations Tests

    [Fact]
    public void GetAllSpecializations_ShouldReturnAllEnumValues()
    {
        // Act
        var result = _service.GetAllSpecializations();

        // Assert
        result.Should().HaveCount(8, "there are 8 specialization types");
        result.Should().ContainKey(WorkerSpecialization.GeneralMaintenance);
        result.Should().ContainKey(WorkerSpecialization.Plumbing);
        result.Should().ContainKey(WorkerSpecialization.Electrical);
        result.Should().ContainKey(WorkerSpecialization.HVAC);
        result.Should().ContainKey(WorkerSpecialization.Carpentry);
        result.Should().ContainKey(WorkerSpecialization.Painting);
        result.Should().ContainKey(WorkerSpecialization.Locksmith);
        result.Should().ContainKey(WorkerSpecialization.ApplianceRepair);
    }

    [Fact]
    public void GetAllSpecializations_ShouldHaveCorrectDisplayNames()
    {
        // Act
        var result = _service.GetAllSpecializations();

        // Assert
        result[WorkerSpecialization.GeneralMaintenance].Should().Be("General Maintenance");
        result[WorkerSpecialization.Plumbing].Should().Be("Plumbing");
        result[WorkerSpecialization.Electrical].Should().Be("Electrical");
        result[WorkerSpecialization.HVAC].Should().Be("HVAC");
    }

    #endregion

    #region Edge Cases and Business Rules

    [Fact]
    public void DetermineRequiredSpecialization_WithMultipleKeywords_ShouldReturnFirstMatch()
    {
        // Arrange - Description contains both "water" (plumbing) and "electric" (electrical)
        // Plumbing is checked first in priority order
        var title = "Multiple issues";
        var description = "Water leak near electric outlet";

        // Act
        var result = _service.DetermineRequiredSpecialization(title, description);

        // Assert
        result.Should().Be(WorkerSpecialization.Plumbing,
            "Plumbing keyword 'water' should match before Electrical keyword 'electric' due to priority order");
    }

    [Fact]
    public void DetermineRequiredSpecialization_WithWhitespaceOnly_ShouldReturnGeneralMaintenance()
    {
        // Act
        var result = _service.DetermineRequiredSpecialization("   ", "   ");

        // Assert
        result.Should().Be(WorkerSpecialization.GeneralMaintenance);
    }

    [Theory]
    [InlineData("Need locksmith", "Door lock stuck", WorkerSpecialization.Locksmith)]
    [InlineData("Wooden door", "Door wooden frame broken, carpenter needed", WorkerSpecialization.Carpentry)]
    public void DetermineRequiredSpecialization_PriorityOrdering_ShouldWork(
        string title, string description, WorkerSpecialization expected)
    {
        // Act
        var result = _service.DetermineRequiredSpecialization(title, description);

        // Assert
        result.Should().Be(expected,
            "priority ordering should ensure more specific keywords match first");
    }

    #endregion
}