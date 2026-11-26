using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fourway.Core.Models;

/// <summary>
/// EF Core konfigurace pro <see cref="RouteIdsInfo"/>.
/// </summary>
public sealed class RouteIdsInfoConfiguration : IEntityTypeConfiguration<RouteIdsInfo>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RouteIdsInfo> builder)
    {
        builder.ToTable("route_ids_info");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.RouteId).IsRequired();
        builder.Property(x => x.IdsSystemId).IsRequired();
        builder.Property(x => x.PublicNumber)
            .HasMaxLength(RouteIdsInfo.PublicNumberMaxLength)
            .IsRequired();
        builder.Property(x => x.FullyIntegrated).IsRequired();

        builder.HasIndex(x => new { x.RouteId, x.IdsSystemId }).IsUnique();

        builder.HasOne<Route>()
            .WithMany()
            .HasForeignKey(x => x.RouteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<IdsSystem>()
            .WithMany()
            .HasForeignKey(x => x.IdsSystemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
