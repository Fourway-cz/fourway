using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fourway.Core.Models;

/// <summary>
/// EF Core konfigurace pro <see cref="IdsSystem"/>.
/// </summary>
public sealed class IdsSystemConfiguration : IEntityTypeConfiguration<IdsSystem>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<IdsSystem> builder)
    {
        builder.ToTable("ids_systems");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Code)
            .HasMaxLength(IdsSystem.CodeMaxLength)
            .IsRequired();
        builder.Property(x => x.Name).HasMaxLength(IdsSystem.NameMaxLength);

        builder.HasIndex(x => x.Code).IsUnique();
    }
}
