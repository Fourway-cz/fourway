using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fourway.Api.Models;

public sealed class ExampleConfiguration : IEntityTypeConfiguration<Example>
{
    public void Configure(EntityTypeBuilder<Example> builder)
    {
        builder.ToTable("examples");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasMaxLength(Example.NameMaxLength)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(Example.DescriptionMaxLength);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasDefaultValue(ExampleStatus.Active);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.Status);

        builder.Ignore(x => x.DisplayName);
    }
}
