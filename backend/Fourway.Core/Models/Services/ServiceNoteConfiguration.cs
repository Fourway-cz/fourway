using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fourway.Core.Models;

/// <summary>
/// EF Core konfigurace pro <see cref="ServiceNote"/>.
/// </summary>
public sealed class ServiceNoteConfiguration : IEntityTypeConfiguration<ServiceNote>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ServiceNote> builder)
    {
        builder.ToTable("service_notes", table =>
            table.HasCheckConstraint(
                "ck_service_notes_sequence_not_negative",
                "sequence >= 0"));

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.ServiceId).IsRequired();
        builder.Property(x => x.Sequence).IsRequired();
        builder.Property(x => x.Label).HasMaxLength(ServiceNote.LabelMaxLength);
        builder.Property(x => x.Text)
            .HasMaxLength(ServiceNote.TextMaxLength)
            .IsRequired();
        builder.Property(x => x.Source).IsRequired();
        builder.Property(x => x.ValidFrom);
        builder.Property(x => x.ValidTo);

        builder.HasOne<Service>()
            .WithMany()
            .HasForeignKey(x => x.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
