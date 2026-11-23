using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fourway.Api.Models;

/// <summary>
/// EF Core konfigurace pro <see cref="Route"/>.
/// </summary>
public sealed class RouteConfiguration : IEntityTypeConfiguration<Route>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Route> builder)
    {
        builder.ToTable("routes", table =>
        {
            table.HasCheckConstraint("ck_routes_variant_not_negative", "variant >= 0");
            table.HasCheckConstraint(
                "ck_routes_validity",
                "valid_to IS NULL OR valid_to >= valid_from");
            table.HasCheckConstraint(
                "ck_routes_gtfs_route_type_not_negative",
                "gtfs_route_type IS NULL OR gtfs_route_type >= 0");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.LicenseNumber)
            .HasMaxLength(Route.LicenseNumberMaxLength)
            .IsRequired();
        builder.Property(x => x.Variant).IsRequired();
        builder.Property(x => x.ValidFrom).IsRequired();
        builder.Property(x => x.ValidTo);
        builder.Property(x => x.ImportBatchId).IsRequired();
        builder.Property(x => x.AgencyId).IsRequired();
        builder.Property(x => x.Name)
            .HasMaxLength(Route.NameMaxLength)
            .IsRequired();
        builder.Property(x => x.LineType).IsRequired();
        builder.Property(x => x.VehicleMode).IsRequired();
        builder.Property(x => x.GtfsRouteType);
        builder.Property(x => x.OneWay).IsRequired();
        builder.Property(x => x.LicenseRef).HasMaxLength(Route.LicenseRefMaxLength);
        builder.Property(x => x.LicenseValidFrom);
        builder.Property(x => x.LicenseValidTo);
        builder.Property(x => x.ApprovingAuthority)
            .HasMaxLength(Route.ApprovingAuthorityMaxLength);

        builder.HasIndex(x => new { x.LicenseNumber, x.Variant, x.ValidFrom }).IsUnique();

        builder.HasOne<ImportBatch>()
            .WithMany()
            .HasForeignKey(x => x.ImportBatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Agency>()
            .WithMany()
            .HasForeignKey(x => x.AgencyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
