using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fourway.Core.Models;

/// <summary>
/// EF Core konfigurace pro <see cref="Trip"/>.
/// </summary>
public sealed class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.ToTable("trips", table =>
            table.HasCheckConstraint("ck_trips_trip_number_positive", "trip_number > 0"));

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.RouteId).IsRequired();
        builder.Property(x => x.TripNumber).IsRequired();
        builder.Property(x => x.ServiceId).IsRequired();
        builder.Property(x => x.WheelchairAccessible).IsRequired();
        builder.Property(x => x.BikesAllowed).IsRequired();
        builder.Property(x => x.LocalTransportExcluded).IsRequired();

        builder.HasIndex(x => new { x.RouteId, x.TripNumber }).IsUnique();

        builder.HasOne<Route>()
            .WithMany()
            .HasForeignKey(x => x.RouteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Service>()
            .WithMany()
            .HasForeignKey(x => x.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(x => x.Direction);
    }
}
