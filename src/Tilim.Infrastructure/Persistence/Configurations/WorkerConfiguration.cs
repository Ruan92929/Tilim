using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tilim.Domain.Entities;
using Tilim.Domain.ValueObjects;

namespace Tilim.Infrastructure.Persistence.Configurations;

public sealed class WorkerConfiguration : IEntityTypeConfiguration<Worker>
{
    public void Configure(EntityTypeBuilder<Worker> builder)
    {
        builder.ToTable("workers");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Slug)
            .HasConversion(slug => slug.Value, value => Slug.Create(value))
            .HasMaxLength(Domain.ValueObjects.Slug.MaxLength)
            .IsRequired();

        builder.HasIndex(w => w.Slug).IsUnique();

        builder.Property(w => w.DisplayName)
            .HasMaxLength(Worker.MaxDisplayNameLength)
            .IsRequired();

        builder.Property(w => w.PixKey)
            .HasMaxLength(140)
            .IsRequired();

        builder.Property(w => w.PixKeyType).HasConversion<string>().HasMaxLength(20);

        builder.Property(w => w.City)
            .HasMaxLength(Worker.MaxCityLength)
            .IsRequired();

        builder.Property(w => w.ThankYouMessage)
            .HasMaxLength(Worker.MaxThankYouMessageLength);

        // Mapped through the backing field as a JSON column — portable across
        // PostgreSQL and SQLite, unlike a native Postgres decimal[] column.
        builder.Property<List<decimal>>("_suggestedAmounts")
            .HasColumnName("suggested_amounts")
            .HasConversion(AmountsConverter())
            .Metadata.SetValueComparer(AmountsComparer());

        builder.Property(w => w.IsActive).IsRequired();
        builder.Property(w => w.CreatedAtUtc).IsRequired();
        builder.Property(w => w.UpdatedAtUtc).IsRequired();
    }

    private static Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<List<decimal>, string> AmountsConverter() =>
        new(
            amounts => JsonSerializer.Serialize(amounts, (JsonSerializerOptions?)null),
            json => JsonSerializer.Deserialize<List<decimal>>(json, (JsonSerializerOptions?)null) ?? new List<decimal>());

    private static ValueComparer<List<decimal>> AmountsComparer() =>
        new(
            (a, b) => (a ?? new()).SequenceEqual(b ?? new()),
            a => a.Aggregate(0, (hash, value) => HashCode.Combine(hash, value)),
            a => a.ToList());
}
