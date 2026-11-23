using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fourway.Api.Models;

/// <summary>
/// EF Core konfigurace pro <see cref="AgencyBranch"/>.
/// </summary>
public sealed class AgencyBranchConfiguration : IEntityTypeConfiguration<AgencyBranch>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AgencyBranch> builder)
    {
        builder.ToTable("agency_branches", table =>
            table.HasCheckConstraint(
                "ck_agency_branches_branch_number_not_negative",
                "branch_number >= 0"));

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.AgencyId).IsRequired();
        builder.Property(x => x.BranchNumber).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(AgencyBranch.AddressMaxLength);
        builder.Property(x => x.PhonePrefix).HasMaxLength(AgencyBranch.PhonePrefixMaxLength);
        builder.Property(x => x.Phone1).HasMaxLength(AgencyBranch.PhoneMaxLength);
        builder.Property(x => x.Phone2).HasMaxLength(AgencyBranch.PhoneMaxLength);
        builder.Property(x => x.Fax).HasMaxLength(AgencyBranch.FaxMaxLength);
        builder.Property(x => x.Email).HasMaxLength(AgencyBranch.EmailMaxLength);
        builder.Property(x => x.Web).HasMaxLength(AgencyBranch.WebMaxLength);

        builder.HasIndex(x => new { x.AgencyId, x.BranchNumber }).IsUnique();

        builder.HasOne<Agency>()
            .WithMany()
            .HasForeignKey(x => x.AgencyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
