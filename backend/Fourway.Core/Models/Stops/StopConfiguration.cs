using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fourway.Core.Models;

/// <summary>
/// EF Core konfigurace pro <see cref="Stop"/>.
/// </summary>
public sealed class StopConfiguration : IEntityTypeConfiguration<Stop>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Stop> builder)
    {
        builder.ToTable("stops", table =>
        {
            table.HasCheckConstraint(
                "ck_stops_district_code_length",
                "char_length(district_code) = 2");
            table.HasCheckConstraint(
                "ck_stops_country_code_length",
                "char_length(country_code) = 2");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Municipality)
            .HasMaxLength(Stop.MunicipalityMaxLength)
            .IsRequired();
        builder.Property(x => x.MunicipalityPart)
            .HasMaxLength(Stop.MunicipalityPartMaxLength);
        builder.Property(x => x.LocalityDetail)
            .HasMaxLength(Stop.LocalityDetailMaxLength);
        builder.Property(x => x.DistrictCode)
            .HasMaxLength(Stop.DistrictCodeMaxLength)
            .IsRequired();
        builder.Property(x => x.CountryCode)
            .HasMaxLength(Stop.CountryCodeMaxLength)
            .IsRequired();

        builder.Property(x => x.Location)
            .HasColumnType($"geography (point, {Stop.LocationSrid})");
        builder.Property(x => x.LocationSource)
            .HasMaxLength(Stop.LocationSourceMaxLength);

        builder.Property(x => x.HasPlatform).IsRequired();
        builder.Property(x => x.WheelchairAccessible).IsRequired();
        builder.Property(x => x.HasShelter).IsRequired();
        builder.Property(x => x.HasInfoBoard).IsRequired();
        builder.Property(x => x.HasLighting).IsRequired();

        builder.Property(x => x.MunicipalityNormalized)
            .HasComputedColumnSql(
                "coalesce(f_unaccent(lower(btrim(municipality))), '')",
                stored: true)
            .IsRequired();
        builder.Property(x => x.MunicipalityPartNormalized)
            .HasComputedColumnSql(
                "coalesce(f_unaccent(lower(btrim(municipality_part))), '')",
                stored: true)
            .IsRequired();
        builder.Property(x => x.LocalityDetailNormalized)
            .HasComputedColumnSql(
                "coalesce(f_unaccent(lower(btrim(locality_detail))), '')",
                stored: true)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.MunicipalityNormalized,
            x.MunicipalityPartNormalized,
            x.LocalityDetailNormalized,
            x.DistrictCode,
            x.CountryCode
        }).IsUnique();

        builder.HasIndex(x => x.Location).HasMethod("gist");

        builder.Ignore(x => x.DisplayName);
        builder.Ignore(x => x.HasLocation);
    }
}
