using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentalRepairs.Domain.Entities;
using RentalRepairs.Domain.ValueObjects;

namespace RentalRepairs.Infrastructure.Persistence.Configurations;

public class WorkerConfiguration : IEntityTypeConfiguration<Worker>
{
    public void Configure(EntityTypeBuilder<Worker> builder)
    {
        builder.ToTable("Workers");

        builder.HasKey(w => w.Id);
        
        // Configure Guid ID
        builder.Property(w => w.Id)
            .IsRequired()
            .ValueGeneratedNever(); // Guid generated in domain

        builder.Property(w => w.Specialization)
            .HasMaxLength(100);

        builder.Property(w => w.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(w => w.Notes)
            .HasMaxLength(2000);

        // Configure PersonContactInfo value object
        builder.OwnsOne(w => w.ContactInfo, contactInfo =>
        {
            contactInfo.Property(c => c.FirstName)
                .HasColumnName("ContactInfoFirstName")
                .HasMaxLength(100)
                .IsRequired();

            contactInfo.Property(c => c.LastName)
                .HasColumnName("ContactInfoLastName")
                .HasMaxLength(100)
                .IsRequired();

            contactInfo.Property(c => c.EmailAddress)
                .HasColumnName("ContactInfoEmailAddress")
                .HasMaxLength(255)
                .IsRequired();

            contactInfo.Property(c => c.MobilePhone)
                .HasColumnName("ContactInfoMobilePhone")
                .HasMaxLength(20);

            // Unique constraint on email address (within the owned entity)
            contactInfo.HasIndex(c => c.EmailAddress)
                .IsUnique()
                .HasDatabaseName("IX_Workers_Email");
        });

        // Index on specialization for efficient filtering
        builder.HasIndex(w => w.Specialization)
            .HasDatabaseName("IX_Workers_Specialization");

        // Index on IsActive for performance
        builder.HasIndex(w => w.IsActive)
            .HasDatabaseName("IX_Workers_IsActive");

        // Configure WorkAssignment as owned entities (value objects stored in separate table)
        builder.OwnsMany(w => w.Assignments, assignment =>
        {
            assignment.ToTable("WorkerAssignments");
            
            assignment.WithOwner().HasForeignKey("WorkerId");
            
            assignment.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnName("Id");
            
            assignment.HasKey("Id");
            
            assignment.Property(a => a.WorkOrderNumber)
                .HasMaxLength(50)
                .IsRequired();
                
            assignment.Property(a => a.ScheduledDate)
                .IsRequired();
                
            assignment.Property(a => a.Notes)
                .HasMaxLength(500);
                
            assignment.Property(a => a.AssignedDate)
                .IsRequired();
                
            assignment.Property(a => a.IsCompleted)
                .IsRequired()
                .HasDefaultValue(false);
                
            assignment.Property(a => a.CompletedDate);
            
            assignment.Property(a => a.CompletedSuccessfully);
            
            assignment.Property(a => a.CompletionNotes)
                .HasMaxLength(1000);

            // Index on work order number for efficient lookups
            assignment.HasIndex(a => a.WorkOrderNumber)
                .HasDatabaseName("IX_WorkerAssignments_WorkOrderNumber");
                
            // Index on scheduled date for efficient filtering
            assignment.HasIndex(a => a.ScheduledDate)
                .HasDatabaseName("IX_WorkerAssignments_ScheduledDate");
        });

        // Configure domain events to be ignored (handled by base configuration)
        builder.Ignore(w => w.DomainEvents);
    }
}