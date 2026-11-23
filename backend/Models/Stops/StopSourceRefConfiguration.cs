using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fourway.Api.Models;

/// <summary>
/// EF Core konfigurace pro <see cref="StopSourceRef"/>.
/// </summary>
public sealed class StopSourceRefConfiguration : IEntityTypeConfiguration<StopSourceRef>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<StopSourceRef> builder)
    {
        builder.ToTable("stop_source_refs");

        builder.HasKey(x => new { x.ImportBatchId, x.SourceStopNo });

        builder.Property(x => x.ImportBatchId).IsRequired();
        builder.Property(x => x.SourceStopNo).IsRequired();
        builder.Property(x => x.StopId).IsRequired();

        builder.HasOne<ImportBatch>()
            .WithMany()
            .HasForeignKey(x => x.ImportBatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Stop>()
            .WithMany()
            .HasForeignKey(x => x.StopId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
