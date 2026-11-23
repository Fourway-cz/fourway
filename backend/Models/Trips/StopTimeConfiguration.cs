using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fourway.Api.Models;

/// <summary>
/// EF Core konfigurace pro <see cref="StopTime"/>.
/// </summary>
public sealed class StopTimeConfiguration : IEntityTypeConfiguration<StopTime>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<StopTime> builder)
    {
        builder.ToTable("stop_times", table =>
        {
            table.HasCheckConstraint(
                "ck_stop_times_stop_sequence_positive",
                "stop_sequence > 0");
            table.HasCheckConstraint(
                "ck_stop_times_tariff_km_not_negative",
                "tariff_km IS NULL OR tariff_km >= 0");
            table.HasCheckConstraint(
                "ck_stop_times_actual_km_not_negative",
                "actual_km IS NULL OR actual_km >= 0");
            table.HasCheckConstraint(
                "ck_stop_times_arrival_max",
                "arrival_time IS NULL OR arrival_time_max IS NULL OR arrival_time_max >= arrival_time");
            table.HasCheckConstraint(
                "ck_stop_times_departure_max",
                "departure_time IS NULL OR departure_time_max IS NULL OR departure_time_max >= departure_time");
        });

        builder.HasKey(x => new { x.TripId, x.StopSequence });

        builder.Property(x => x.TripId).IsRequired();
        builder.Property(x => x.StopSequence).IsRequired();
        builder.Property(x => x.RouteStopId).IsRequired();
        builder.Property(x => x.StopId).IsRequired();
        builder.Property(x => x.PlatformCode)
            .HasMaxLength(StopTime.PlatformCodeMaxLength);

        builder.Property(x => x.ArrivalTime).HasColumnType("interval");
        builder.Property(x => x.DepartureTime).HasColumnType("interval");
        builder.Property(x => x.ArrivalTimeMax).HasColumnType("interval");
        builder.Property(x => x.DepartureTimeMax).HasColumnType("interval");

        builder.Property(x => x.StopKind).IsRequired();
        builder.Property(x => x.RequestStop).IsRequired();
        builder.Property(x => x.BoardFront).IsRequired();
        builder.Property(x => x.TariffKm);
        builder.Property(x => x.ActualKm);

        builder.HasOne<Trip>()
            .WithMany()
            .HasForeignKey(x => x.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<RouteStop>()
            .WithMany()
            .HasForeignKey(x => x.RouteStopId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Stop>()
            .WithMany()
            .HasForeignKey(x => x.StopId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(x => x.HasArrival);
        builder.Ignore(x => x.HasDeparture);
        builder.Ignore(x => x.IsPassThrough);
    }
}
