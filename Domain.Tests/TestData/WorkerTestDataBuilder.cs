using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.ValueObjects;

namespace RentalRepairs.Domain.Tests.TestData;

/// <summary>
/// Test data builder for creating Worker entities with fluent interface
/// </summary>
public class WorkerTestDataBuilder
{
    private PersonContactInfo _contactInfo = new("Bob", "Builder", "bob@workers.com", "555-9999");
    private string? _specialization = null;
    private List<string> _notes = new();
    private bool _isActive = true;

    public WorkerTestDataBuilder WithContactInfo(PersonContactInfo contactInfo)
    {
        _contactInfo = contactInfo;
        return this;
    }

    public WorkerTestDataBuilder WithContactInfo(string firstName, string lastName, string email, string? phone = null)
    {
        _contactInfo = new PersonContactInfo(firstName, lastName, email, phone);
        return this;
    }

    public WorkerTestDataBuilder WithSpecialization(string specialization)
    {
        _specialization = specialization;
        return this;
    }

    public WorkerTestDataBuilder WithNotes(params string[] notes)
    {
        _notes = notes.ToList();
        return this;
    }

    public WorkerTestDataBuilder AsInactive()
    {
        _isActive = false;
        return this;
    }

    public WorkerTestDataBuilder AsPlumber() => WithSpecialization("Plumbing");
    public WorkerTestDataBuilder AsElectrician() => WithSpecialization("Electrical");
    public WorkerTestDataBuilder AsHVACTechnician() => WithSpecialization("HVAC");
    public WorkerTestDataBuilder AsPainter() => WithSpecialization("Painting");
    public WorkerTestDataBuilder AsGeneralMaintenance() => WithSpecialization("General Maintenance");

    public Worker Build()
    {
        var worker = new Worker(_contactInfo);

        if (!string.IsNullOrEmpty(_specialization))
        {
            worker.SetSpecialization(_specialization);
        }

        foreach (var note in _notes)
        {
            worker.AddNotes(note);
        }

        if (!_isActive)
        {
            worker.Deactivate();
        }

        return worker;
    }

    public static WorkerTestDataBuilder Default() => new WorkerTestDataBuilder();

    public static WorkerTestDataBuilder ForPlumber() => new WorkerTestDataBuilder()
        .WithContactInfo("Mike", "Pipeson", "mike.pipes@workers.com", "555-1111")
        .AsPlumber()
        .WithNotes("Experienced with residential plumbing", "Available 24/7 for emergencies");

    public static WorkerTestDataBuilder ForElectrician() => new WorkerTestDataBuilder()
        .WithContactInfo("Sarah", "Voltage", "sarah.voltage@workers.com", "555-2222")
        .AsElectrician()
        .WithNotes("Licensed electrician", "Specializes in residential wiring");

    public static WorkerTestDataBuilder ForHVACTechnician() => new WorkerTestDataBuilder()
        .WithContactInfo("Tom", "Heater", "tom.heater@workers.com", "555-3333")
        .AsHVACTechnician()
        .WithNotes("HVAC certified", "10+ years experience");

    public static WorkerTestDataBuilder ForGeneralMaintenance() => new WorkerTestDataBuilder()
        .WithContactInfo("Jack", "Handyman", "jack.handyman@workers.com", "555-4444")
        .AsGeneralMaintenance()
        .WithNotes("Multi-skilled maintenance worker");

    public static WorkerTestDataBuilder ForInactiveWorker() => new WorkerTestDataBuilder()
        .WithContactInfo("Retired", "Worker", "retired@workers.com")
        .AsInactive()
        .WithNotes("No longer available for assignments");
}