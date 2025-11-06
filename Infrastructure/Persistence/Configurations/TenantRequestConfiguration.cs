using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Infrastructure.Persistence.Configurations;

public class TenantRequestConfiguration : IEntityTypeConfiguration<TenantRequest>
{
    public void Configure(EntityTypeBuilder<TenantRequest> builder)
    {
        builder.ToTable("TenantRequests");

        builder.HasKey(tr => tr.Id);

        // Configure Guid ID
        builder.Property(tr => tr.Id)
            .IsRequired()
            .ValueGeneratedNever(); // Guid generated in domain

        builder.Property(tr => tr.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(tr => tr.Code)
            .IsUnique();

        builder.Property(tr => tr.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(tr => tr.Description)
            .IsRequired()
            .HasMaxLength(1000); // Updated to match domain constant

        builder.Property(tr => tr.UrgencyLevel)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(tr => tr.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(tr => tr.IsEmergency)
            .IsRequired()
            .HasDefaultValue(false);

        // Configure explicit foreign key properties
        builder.Property(tr => tr.TenantId)
            .IsRequired();

        builder.Property(tr => tr.PropertyId)
            .IsRequired();

        // Configure denormalized fields from domain model
        builder.Property(tr => tr.TenantFullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(tr => tr.TenantEmail)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(tr => tr.TenantUnit)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(tr => tr.PropertyName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(tr => tr.PropertyPhone)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(tr => tr.SuperintendentFullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(tr => tr.SuperintendentEmail)
            .IsRequired()
            .HasMaxLength(255);

        // Configure worker assignment properties
        builder.Property(tr => tr.ScheduledDate)
            .IsRequired(false);

        builder.Property(tr => tr.AssignedWorkerEmail)
            .HasMaxLength(256)
            .IsRequired(false);

        builder.Property(tr => tr.AssignedWorkerName)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(tr => tr.WorkOrderNumber)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(tr => tr.CompletedDate)
            .IsRequired(false);

        builder.Property(tr => tr.WorkCompletedSuccessfully)
            .IsRequired(false);

        builder.Property(tr => tr.CompletionNotes)
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.Property(tr => tr.ClosureNotes)
            .HasMaxLength(2000)
            .IsRequired(false);

        // Configure tenant preferences
        builder.Property(tr => tr.PreferredContactTime)
            .HasMaxLength(100)
            .IsRequired(false);

        // Configure indexes for common queries
        builder.HasIndex(tr => tr.Status)
            .HasDatabaseName("IX_TenantRequests_Status");

        builder.HasIndex(tr => tr.UrgencyLevel)
            .HasDatabaseName("IX_TenantRequests_UrgencyLevel");

        builder.HasIndex(tr => tr.TenantId)
            .HasDatabaseName("IX_TenantRequests_TenantId");

        builder.HasIndex(tr => tr.PropertyId)
            .HasDatabaseName("IX_TenantRequests_PropertyId");

        builder.HasIndex(tr => tr.CreatedAt)
            .HasDatabaseName("IX_TenantRequests_CreatedAt");

        builder.HasIndex(tr => tr.AssignedWorkerEmail)
            .HasDatabaseName("IX_TenantRequests_AssignedWorkerEmail");

        builder.HasIndex(tr => tr.ScheduledDate)
            .HasDatabaseName("IX_TenantRequests_ScheduledDate");

        builder.HasIndex(tr => tr.WorkOrderNumber)
            .HasDatabaseName("IX_TenantRequests_WorkOrderNumber");

        builder.HasIndex(tr => tr.IsEmergency)
            .HasDatabaseName("IX_TenantRequests_IsEmergency");

        // Configure domain events to be ignored (handled by base configuration)  
        builder.Ignore(tr => tr.DomainEvents);
    }
}