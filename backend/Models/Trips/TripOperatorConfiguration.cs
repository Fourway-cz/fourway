using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fourway.Api.Models;

/// <summary>
/// EF Core konfigurace pro <see cref="TripOperator"/>.
/// </summary>
public sealed class TripOperatorConfiguration : IEntityTypeConfiguration<TripOperator>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TripOperator> builder)
    {
        builder.ToTable("trip_operators", table =>
            table.HasCheckConstraint(
                "ck_trip_operators_validity",
                "valid_from IS NULL OR valid_to IS NULL OR valid_to >= valid_from"));

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.TripId).IsRequired();
        builder.Property(x => x.AgencyId).IsRequired();
        builder.Property(x => x.ValidFrom);
        builder.Property(x => x.ValidTo);

        builder.HasIndex(x => new { x.TripId, x.AgencyId, x.ValidFrom })
            .IsUnique()
            .AreNullsDistinct(false);

        builder.HasOne<Trip>()
            .WithMany()
            .HasForeignKey(x => x.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Agency>()
            .WithMany()
            .HasForeignKey(x => x.AgencyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
