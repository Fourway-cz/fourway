using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fourway.Core.Models;

/// <summary>
/// EF Core konfigurace pro <see cref="ImportBatch"/>.
/// </summary>
public sealed class ImportBatchConfiguration : IEntityTypeConfiguration<ImportBatch>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ImportBatch> builder)
    {
        builder.ToTable("import_batches");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.SourcePath)
            .HasMaxLength(ImportBatch.SourcePathMaxLength)
            .IsRequired();

        builder.Property(x => x.FileHash)
            .HasMaxLength(ImportBatch.FileHashMaxLength)
            .IsRequired();

        builder.Property(x => x.JdfVersion)
            .HasMaxLength(ImportBatch.JdfVersionMaxLength)
            .IsRequired();

        builder.Property(x => x.ExportedOn);
        builder.Property(x => x.ImportedAt).IsRequired();

        builder.HasIndex(x => x.FileHash).IsUnique();
    }
}
