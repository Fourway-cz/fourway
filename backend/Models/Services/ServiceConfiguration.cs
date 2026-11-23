using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fourway.Api.Models;

/// <summary>
/// EF Core konfigurace pro <see cref="Service"/>.
/// </summary>
public sealed class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.ToTable("services", table =>
            table.HasCheckConstraint("ck_services_rule_hash_not_empty", "rule_hash <> ''"));

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.RuleHash)
            .HasMaxLength(Service.RuleHashMaxLength)
            .IsRequired();

        builder.HasIndex(x => x.RuleHash).IsUnique();

        builder.HasMany(x => x.Patterns)
            .WithOne()
            .HasForeignKey(x => x.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Exceptions)
            .WithOne()
            .HasForeignKey(x => x.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
