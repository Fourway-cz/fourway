namespace Fourway.Import.Parsing.Models;

public sealed record RawVerzeJdf(
    string CisloVerze,
    string? IdentifikaceDavky,
    string? PopisDavky,
    string? OznaceniZpracovatele,
    DateOnly? DatumExportu);

public sealed record RawDopravce(
    string? IcDopravce,
    string? DicDopravce,
    string NazevDopravce,
    int DruhDopravce,
    string? JmenoFyzickeOsoby,
    string? SidloProvozovna,
    string? TelefonPredvolba,
    string? Telefon1,
    string? Telefon2,
    string? Email,
    string? Web,
    int CisloDopravce);

public sealed record RawLinka(
    string CisloLinky,
    string NazevLinky,
    string? IcDopravce,
    string TypLinky,
    string DopravniProstredek,
    bool JednosmernyProvoz,
    DateOnly? PlatnostLicenceOd,
    DateOnly? PlatnostLicenceDo,
    string? CisloLicence,
    string? SchvalujiciOrgan,
    DateOnly PlatnostOd,
    DateOnly? PlatnostDo,
    int CisloDopravce,
    short RozliseniLinky);

public sealed record RawZastavka(
    int CisloZastavky,
    string NazevObce,
    string? CastObce,
    string? BlizsiUrceni,
    string Okres,
    string Stat,
    IReadOnlyList<int> PevneKody);

public sealed record RawZaslinka(
    string CisloLinky,
    int TarifniCislo,
    string? TarifniPasmo,
    int CisloZastavky,
    IReadOnlyList<int> PevneKody,
    short RozliseniLinky);

public sealed record RawSpoj(
    string CisloLinky,
    int CisloSpoje,
    IReadOnlyList<int> PevneKody,
    short RozliseniLinky);

public sealed record RawZasspoj(
    string CisloLinky,
    int CisloSpoje,
    int TarifniCislo,
    int CisloZastavky,
    string? CisloStanoviste,
    IReadOnlyList<int> PevneKody,
    int? TarifniKm,
    int? SkutecneKm,
    string CasPrijezduMinRaw,
    string CasOdjezduMinRaw,
    string CasPrijezduMaxRaw,
    string CasOdjezduMaxRaw,
    short RozliseniLinky);

public sealed record RawCaskod(
    string CisloLinky,
    int CisloSpoje,
    int IndexCaskodu,
    string? OznaceniCaskodu,
    int TypCaskodu,
    DateOnly? DatumOd,
    DateOnly? DatumDo,
    string? TextPoznamky,
    short RozliseniLinky);

public sealed record RawLinExt(
    string CisloLinky,
    int PoradoveCislo,
    string KodDopravy,
    string OznaceniLinky,
    bool PriznakIntegrace,
    short RozliseniLinky);

public sealed record RawAltDop(
    string CisloLinky,
    int CisloSpoje,
    string? IcDopravce,
    DateOnly? PlatnostOd,
    DateOnly? PlatnostDo,
    int CisloDopravce,
    short RozliseniLinky);
