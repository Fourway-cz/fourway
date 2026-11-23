using NetTopologySuite.Geometries;

namespace Fourway.Api.Models;

/// <summary>
/// Kanonická fyzická zastávka.
/// </summary>
/// <remarks>
/// Zastávky se deduplikují podle normalizovaného tvaru názvu a administrativních
/// údajů. Raw hodnoty se ukládají kvůli zobrazení, normalizované hodnoty
/// dopočítává databáze a tvoří unikátní klíč.
/// </remarks>
public sealed class Stop
{
    /// <summary>Maximální délka názvu obce.</summary>
    public const int MunicipalityMaxLength = 200;

    /// <summary>Maximální délka části obce.</summary>
    public const int MunicipalityPartMaxLength = 200;

    /// <summary>Maximální délka bližšího určení zastávky.</summary>
    public const int LocalityDetailMaxLength = 200;

    /// <summary>Maximální délka kódu okresu.</summary>
    public const int DistrictCodeMaxLength = 2;

    /// <summary>Maximální délka kódu státu.</summary>
    public const int CountryCodeMaxLength = 2;

    /// <summary>Maximální délka popisu zdroje lokace.</summary>
    public const int LocationSourceMaxLength = 100;

    /// <summary>SRID používaný pro geografické body.</summary>
    public const int LocationSrid = 4326;

    /// <summary>Identifikátor zastávky.</summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Název obce z pole <c>NazevObce</c> v <c>Zastavky.txt</c>.
    /// </summary>
    public string Municipality { get; private set; } = null!;

    /// <summary>
    /// Část obce z pole <c>CastObce</c>, pokud je uvedená.
    /// </summary>
    public string? MunicipalityPart { get; private set; }

    /// <summary>
    /// Bližší určení zastávky z pole <c>BlizsiUrceni</c>, například <c>aut.st.</c>.
    /// </summary>
    public string? LocalityDetail { get; private set; }

    /// <summary>
    /// Kód okresu, historicky bývalá SPZ.
    /// </summary>
    public string DistrictCode { get; private set; } = null!;

    /// <summary>
    /// Dvoupísmenný kód státu, například <c>CZ</c>.
    /// </summary>
    public string CountryCode { get; private set; } = null!;

    /// <summary>
    /// Geografická poloha zastávky jako bod WGS84, pokud je známá.
    /// </summary>
    /// <remarks>
    /// JDF souřadnice neobsahuje, sloupec je připravený pro pozdější doplnění.
    /// Bod se vytváří jako <c>Point(longitude, latitude)</c>.
    /// </remarks>
    public Point? Location { get; private set; }

    /// <summary>
    /// Popis zdroje nebo způsobu získání lokace.
    /// </summary>
    public string? LocationSource { get; private set; }

    /// <summary>
    /// Označuje, zda zastávka má nástupiště podle pevných kódů vybavenosti.
    /// </summary>
    /// <remarks>
    /// Extrahuje se z <c>PevnyKod1_5</c> v <c>Zastavky.txt</c>.
    /// </remarks>
    public bool HasPlatform { get; private set; }

    /// <summary>
    /// Označuje bezbariérovost zastávky podle pevných kódů vybavenosti.
    /// </summary>
    /// <remarks>
    /// Extrahuje se z <c>PevnyKod1_5</c> v <c>Zastavky.txt</c>.
    /// </remarks>
    public bool WheelchairAccessible { get; private set; }

    /// <summary>
    /// Označuje, zda zastávka má přístřešek podle pevných kódů vybavenosti.
    /// </summary>
    /// <remarks>
    /// Extrahuje se z <c>PevnyKod1_5</c> v <c>Zastavky.txt</c>.
    /// </remarks>
    public bool HasShelter { get; private set; }

    /// <summary>
    /// Označuje, zda zastávka má informační tabuli podle pevných kódů vybavenosti.
    /// </summary>
    /// <remarks>
    /// Extrahuje se z <c>PevnyKod1_5</c> v <c>Zastavky.txt</c>.
    /// </remarks>
    public bool HasInfoBoard { get; private set; }

    /// <summary>
    /// Označuje, zda zastávka má osvětlení podle pevných kódů vybavenosti.
    /// </summary>
    /// <remarks>
    /// Extrahuje se z <c>PevnyKod1_5</c> v <c>Zastavky.txt</c>.
    /// </remarks>
    public bool HasLighting { get; private set; }

    /// <summary>
    /// Normalizovaný tvar <see cref="Municipality"/> pro deduplikaci.
    /// </summary>
    /// <remarks>
    /// Generuje databáze výrazem <c>coalesce(f_unaccent(lower(btrim(municipality))), '')</c>.
    /// Nikdy se nezobrazuje uživateli a nemění se ručně.
    /// </remarks>
    public string MunicipalityNormalized { get; private set; } = null!;

    /// <summary>
    /// Normalizovaný tvar <see cref="MunicipalityPart"/> pro deduplikaci.
    /// </summary>
    /// <remarks>
    /// Generuje databáze; prázdná hodnota je uložena jako prázdný řetězec.
    /// </remarks>
    public string MunicipalityPartNormalized { get; private set; } = null!;

    /// <summary>
    /// Normalizovaný tvar <see cref="LocalityDetail"/> pro deduplikaci.
    /// </summary>
    /// <remarks>
    /// Generuje databáze; prázdná hodnota je uložena jako prázdný řetězec.
    /// </remarks>
    public string LocalityDetailNormalized { get; private set; } = null!;

    /// <summary>
    /// Zobrazovaný název zastávky složený z obce, části obce a bližšího určení.
    /// </summary>
    /// <remarks>
    /// Odvozená hodnota, neukládá se do databáze.
    /// </remarks>
    public string DisplayName
    {
        get
        {
            var parts = new List<string> { Municipality };

            if (!string.IsNullOrWhiteSpace(MunicipalityPart) &&
                !string.Equals(MunicipalityPart, Municipality, StringComparison.OrdinalIgnoreCase))
            {
                parts.Add(MunicipalityPart);
            }

            if (!string.IsNullOrWhiteSpace(LocalityDetail))
            {
                parts.Add(LocalityDetail);
            }

            return string.Join(", ", parts);
        }
    }

    /// <summary>
    /// Označuje, zda má zastávka vyplněnou geografickou polohu.
    /// </summary>
    public bool HasLocation => Location is not null && !Location.IsEmpty;

    // Pro EF Core. V doménovém kódu se nepoužívá.
    private Stop()
    {
    }

    /// <summary>
    /// Vytvoří novou zastávku.
    /// </summary>
    public Stop(
        string municipality,
        string? municipalityPart,
        string? localityDetail,
        string districtCode,
        string countryCode,
        Point? location = null,
        string? locationSource = null,
        bool hasPlatform = false,
        bool wheelchairAccessible = false,
        bool hasShelter = false,
        bool hasInfoBoard = false,
        bool hasLighting = false)
    {
        Id = Guid.CreateVersion7();
        Municipality = DomainValidation.Required(
            municipality,
            nameof(municipality),
            MunicipalityMaxLength);
        MunicipalityPart = DomainValidation.Optional(
            municipalityPart,
            nameof(municipalityPart),
            MunicipalityPartMaxLength);
        LocalityDetail = DomainValidation.Optional(
            localityDetail,
            nameof(localityDetail),
            LocalityDetailMaxLength);
        DistrictCode = DomainValidation.Required(
            districtCode,
            nameof(districtCode),
            DistrictCodeMaxLength).ToUpperInvariant();
        CountryCode = DomainValidation.Required(
            countryCode,
            nameof(countryCode),
            CountryCodeMaxLength).ToUpperInvariant();
        SetLocation(location, locationSource);
        HasPlatform = hasPlatform;
        WheelchairAccessible = wheelchairAccessible;
        HasShelter = hasShelter;
        HasInfoBoard = hasInfoBoard;
        HasLighting = hasLighting;
    }

    /// <summary>
    /// Změní raw části názvu zastávky. Normalizované hodnoty dopočítá databáze.
    /// </summary>
    public void Rename(
        string municipality,
        string? municipalityPart,
        string? localityDetail,
        string districtCode,
        string countryCode)
    {
        Municipality = DomainValidation.Required(
            municipality,
            nameof(municipality),
            MunicipalityMaxLength);
        MunicipalityPart = DomainValidation.Optional(
            municipalityPart,
            nameof(municipalityPart),
            MunicipalityPartMaxLength);
        LocalityDetail = DomainValidation.Optional(
            localityDetail,
            nameof(localityDetail),
            LocalityDetailMaxLength);
        DistrictCode = DomainValidation.Required(
            districtCode,
            nameof(districtCode),
            DistrictCodeMaxLength).ToUpperInvariant();
        CountryCode = DomainValidation.Required(
            countryCode,
            nameof(countryCode),
            CountryCodeMaxLength).ToUpperInvariant();
    }

    /// <summary>
    /// Nastaví nebo odebere geografickou polohu zastávky.
    /// </summary>
    /// <param name="location">Bod WGS84, nebo <see langword="null"/>.</param>
    /// <param name="locationSource">Popis zdroje lokace.</param>
    public void SetLocation(Point? location, string? locationSource = null)
    {
        if (location is not null)
        {
            if (location.IsEmpty)
            {
                throw new ArgumentException("Lokace nesmí být prázdný bod.", nameof(location));
            }

            if (location.SRID != LocationSrid)
            {
                throw new ArgumentException(
                    $"Lokace musí mít SRID {LocationSrid}.",
                    nameof(location));
            }
        }

        Location = location;
        LocationSource = DomainValidation.Optional(
            locationSource,
            nameof(locationSource),
            LocationSourceMaxLength);
    }

    /// <summary>
    /// Nastaví příznaky vybavenosti zastávky z pevných kódů.
    /// </summary>
    /// <remarks>
    /// Pevné kódy se čtou z <c>PevnyKod1_5</c> v <c>Zastavky.txt</c>.
    /// </remarks>
    public void SetFacilities(
        bool hasPlatform,
        bool wheelchairAccessible,
        bool hasShelter,
        bool hasInfoBoard,
        bool hasLighting)
    {
        HasPlatform = hasPlatform;
        WheelchairAccessible = wheelchairAccessible;
        HasShelter = hasShelter;
        HasInfoBoard = hasInfoBoard;
        HasLighting = hasLighting;
    }
}
