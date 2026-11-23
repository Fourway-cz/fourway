using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fourway.Core.Models;

/// <summary>
/// EF Core konfigurace pro <see cref="Agency"/>.
/// </summary>
public sealed class AgencyConfiguration : IEntityTypeConfiguration<Agency>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Agency> builder)
    {
        builder.ToTable("agencies");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Ico).HasMaxLength(Agency.IcoMaxLength);
        builder.Property(x => x.Dic).HasMaxLength(Agency.DicMaxLength);
        builder.Property(x => x.Name)
            .HasMaxLength(Agency.NameMaxLength)
            .IsRequired();
        builder.Property(x => x.LegalForm).IsRequired();
        builder.Property(x => x.PersonName).HasMaxLength(Agency.PersonNameMaxLength);

        builder.HasIndex(x => x.Ico).IsUnique();
        builder.Ignore(x => x.IsLegalPerson);
    }
}
