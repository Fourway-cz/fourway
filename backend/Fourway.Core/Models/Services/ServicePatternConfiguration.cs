using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fourway.Core.Models;

/// <summary>
/// EF Core konfigurace pro <see cref="ServicePattern"/>.
/// </summary>
public sealed class ServicePatternConfiguration : IEntityTypeConfiguration<ServicePattern>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ServicePattern> builder)
    {
        builder.ToTable("service_patterns", table =>
        {
            table.HasCheckConstraint(
                "ck_service_patterns_validity",
                "valid_to >= valid_from");
            table.HasCheckConstraint(
                "ck_service_patterns_has_day",
                "monday OR tuesday OR wednesday OR thursday OR friday OR saturday OR sunday");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.ServiceId).IsRequired();
        builder.Property(x => x.Monday).IsRequired();
        builder.Property(x => x.Tuesday).IsRequired();
        builder.Property(x => x.Wednesday).IsRequired();
        builder.Property(x => x.Thursday).IsRequired();
        builder.Property(x => x.Friday).IsRequired();
        builder.Property(x => x.Saturday).IsRequired();
        builder.Property(x => x.Sunday).IsRequired();
        builder.Property(x => x.ValidFrom).IsRequired();
        builder.Property(x => x.ValidTo).IsRequired();
        builder.Property(x => x.WeekParity).IsRequired();
        builder.Property(x => x.ParityAnchor);

        builder.HasIndex(x => new { x.ServiceId, x.ValidFrom, x.ValidTo });

        builder.Ignore(x => x.HasAnyDay);
    }
}
