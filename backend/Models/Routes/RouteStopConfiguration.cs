using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fourway.Api.Models;

/// <summary>
/// EF Core konfigurace pro <see cref="RouteStop"/>.
/// </summary>
public sealed class RouteStopConfiguration : IEntityTypeConfiguration<RouteStop>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RouteStop> builder)
    {
        builder.ToTable("route_stops", table =>
            table.HasCheckConstraint("ck_route_stops_sequence_positive", "sequence > 0"));

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.RouteId).IsRequired();
        builder.Property(x => x.Sequence).IsRequired();
        builder.Property(x => x.StopId).IsRequired();
        builder.Property(x => x.TariffZones)
            .HasColumnType("text[]")
            .IsRequired();
        builder.Property(x => x.RequestStop).IsRequired();

        builder.HasIndex(x => new { x.RouteId, x.Sequence }).IsUnique();

        builder.HasOne<Route>()
            .WithMany()
            .HasForeignKey(x => x.RouteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Stop>()
            .WithMany()
            .HasForeignKey(x => x.StopId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
