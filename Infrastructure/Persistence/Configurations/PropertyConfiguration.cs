using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentalRepairs.Domain.Entities;

namespace RentalRepairs.Infrastructure.Persistence.Configurations;

public class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("Properties");

        builder.HasKey(p => p.Id);

        // Configure Guid ID
        builder.Property(p => p.Id)
            .IsRequired()
            .ValueGeneratedNever(); // Guid generated in domain

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(p => p.Code)
            .IsUnique();

        builder.Property(p => p.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(p => p.NoReplyEmailAddress)
            .IsRequired()
            .HasMaxLength(255);

        // Configure PropertyAddress value object
        builder.OwnsOne(p => p.Address, address =>
        {
            address.Property(a => a.StreetNumber)
                .HasColumnName("AddressStreetNumber")
                .HasMaxLength(20)
                .IsRequired();

            address.Property(a => a.StreetName)
                .HasColumnName("AddressStreetName")
                .HasMaxLength(100)
                .IsRequired();

            address.Property(a => a.City)
                .HasColumnName("AddressCity")
                .HasMaxLength(100)
                .IsRequired();

            address.Property(a => a.PostalCode)
                .HasColumnName("AddressPostalCode")
                .HasMaxLength(20)
                .IsRequired();
        });

        // Configure PersonContactInfo value object for Superintendent
        builder.OwnsOne(p => p.Superintendent, superintendent =>
        {
            superintendent.Property(s => s.FirstName)
                .HasColumnName("SuperintendentFirstName")
                .HasMaxLength(100)
                .IsRequired();

            superintendent.Property(s => s.LastName)
                .HasColumnName("SuperintendentLastName")
                .HasMaxLength(100)
                .IsRequired();

            superintendent.Property(s => s.EmailAddress)
                .HasColumnName("SuperintendentEmailAddress")
                .HasMaxLength(255)
                .IsRequired();

            superintendent.Property(s => s.MobilePhone)
                .HasColumnName("SuperintendentMobilePhone")
                .HasMaxLength(20);
        });

        // Configure Units as string with proper value converter that handles null/empty cases
        builder.Property(p => p.Units)
            .HasConversion(
                // Convert List<string> to string - handle null/empty cases
                v => v != null && v.Any() ? string.Join(';', v) : string.Empty,
                // Convert string to List<string> - handle null/empty cases
                v => string.IsNullOrEmpty(v)
                    ? new List<string>()
                    : v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList())
            .HasColumnName("Units")
            .HasMaxLength(1000);

        // Add value comparer for the Units collection with proper null handling
        builder.Property(p => p.Units)
            .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
                (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c == null ? new List<string>() : c.ToList()));

        // Configure domain events to be ignored (handled by base configuration)
        builder.Ignore(p => p.DomainEvents);
    }
}