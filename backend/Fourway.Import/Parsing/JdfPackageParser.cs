using System.IO.Compression;
using Fourway.Import.Parsing.Csv;
using Fourway.Import.Parsing.Models;

namespace Fourway.Import.Parsing;

public sealed class JdfPackage
{
    public required string ArchiveName { get; init; }
    public required string FileHash { get; init; }
    public RawVerzeJdf? Verze { get; set; }
    public List<RawDopravce> Dopravci { get; } = [];
    public List<RawLinka> Linky { get; } = [];
    public List<RawZastavka> Zastavky { get; } = [];
    public List<RawZaslinka> Zaslinky { get; } = [];
    public List<RawSpoj> Spoje { get; } = [];
    public List<RawZasspoj> Zasspoje { get; } = [];
    public List<RawCaskod> Caskody { get; } = [];
    public List<RawLinExt> LinExt { get; } = [];
    public List<RawAltDop> AltDop { get; } = [];
}

public static class JdfPackageParser
{
    public static JdfPackage Parse(ZipArchive archive, string archiveName, string fileHash)
    {
        var package = new JdfPackage
        {
            ArchiveName = archiveName,
            FileHash = fileHash
        };

        foreach (var entry in archive.Entries)
        {
            var fileName = Path.GetFileName(entry.FullName);
            if (string.IsNullOrEmpty(fileName))
            {
                continue;
            }

            using var stream = entry.Open();

            if (fileName.Equals("VerzeJDF.txt", StringComparison.OrdinalIgnoreCase))
            {
                package.Verze = ParseVerze(stream);
            }
            else if (fileName.Equals("Dopravci.txt", StringComparison.OrdinalIgnoreCase))
            {
                package.Dopravci.AddRange(ParseDopravci(stream));
            }
            else if (fileName.Equals("Linky.txt", StringComparison.OrdinalIgnoreCase))
            {
                package.Linky.AddRange(ParseLinky(stream));
            }
            else if (fileName.Equals("Zastavky.txt", StringComparison.OrdinalIgnoreCase))
            {
                package.Zastavky.AddRange(ParseZastavky(stream));
            }
            else if (fileName.Equals("Zaslinky.txt", StringComparison.OrdinalIgnoreCase))
            {
                package.Zaslinky.AddRange(ParseZaslinky(stream));
            }
            else if (fileName.Equals("Spoje.txt", StringComparison.OrdinalIgnoreCase))
            {
                package.Spoje.AddRange(ParseSpoje(stream));
            }
            else if (fileName.Equals("Zasspoje.txt", StringComparison.OrdinalIgnoreCase))
            {
                package.Zasspoje.AddRange(ParseZasspoje(stream));
            }
            else if (fileName.Equals("Caskody.txt", StringComparison.OrdinalIgnoreCase))
            {
                package.Caskody.AddRange(ParseCaskody(stream));
            }
            else if (fileName.Equals("LinExt.txt", StringComparison.OrdinalIgnoreCase))
            {
                package.LinExt.AddRange(ParseLinExt(stream));
            }
            else if (fileName.Equals("Altdop.txt", StringComparison.OrdinalIgnoreCase))
            {
                package.AltDop.AddRange(ParseAltDop(stream));
            }
        }

        return package;
    }

    private static RawVerzeJdf? ParseVerze(Stream stream)
    {
        foreach (var record in JdfCsvReader.ReadRecords(stream))
        {
            if (record.Count < 5) continue;
            return new RawVerzeJdf(
                CisloVerze: record[0],
                IdentifikaceDavky: NullIfEmpty(record[1]),
                PopisDavky: NullIfEmpty(record[2]),
                OznaceniZpracovatele: NullIfEmpty(record[3]),
                DatumExportu: ParseDate(record[4]));
        }

        return null;
    }

    private static IEnumerable<RawDopravce> ParseDopravci(Stream stream)
    {
        foreach (var record in JdfCsvReader.ReadRecords(stream))
        {
            if (record.Count < 13) continue;

            yield return new RawDopravce(
                IcDopravce: NullIfEmpty(record[0]),
                DicDopravce: NullIfEmpty(record[1]),
                NazevDopravce: record[2],
                DruhDopravce: int.TryParse(record[3], out var d) ? d : 1,
                JmenoFyzickeOsoby: NullIfEmpty(record[4]),
                SidloProvozovna: NullIfEmpty(record[5]),
                TelefonPredvolba: NullIfEmpty(record[6]),
                Telefon1: NullIfEmpty(record[7]),
                Telefon2: NullIfEmpty(record[8]),
                Email: NullIfEmpty(record[10]),
                Web: NullIfEmpty(record[11]),
                CisloDopravce: int.TryParse(record[12], out var cd) ? cd : 1);
        }
    }

    private static IEnumerable<RawLinka> ParseLinky(Stream stream)
    {
        foreach (var record in JdfCsvReader.ReadRecords(stream))
        {
            if (record.Count < 17) continue;

            var validFrom = ParseDate(record[13]);
            if (validFrom is null) continue;

            yield return new RawLinka(
                CisloLinky: record[0],
                NazevLinky: record[1],
                IcDopravce: NullIfEmpty(record[2]),
                TypLinky: record[3],
                DopravniProstredek: record[4],
                JednosmernyProvoz: record[5] == "1",
                PlatnostLicenceOd: ParseDate(record[9]),
                PlatnostLicenceDo: ParseDate(record[10]),
                CisloLicence: NullIfEmpty(record[11]),
                SchvalujiciOrgan: NullIfEmpty(record[12]),
                PlatnostOd: validFrom.Value,
                PlatnostDo: ParseDate(record[14]),
                CisloDopravce: int.TryParse(record[15], out var cd) ? cd : 1,
                RozliseniLinky: short.TryParse(record[16], out var rl) ? rl : (short)1);
        }
    }

    private static IEnumerable<RawZastavka> ParseZastavky(Stream stream)
    {
        foreach (var record in JdfCsvReader.ReadRecords(stream))
        {
            if (record.Count < 6 || !int.TryParse(record[0], out var id)) continue;

            var pevneKody = new List<int>();
            for (var i = 6; i < Math.Min(11, record.Count); i++)
            {
                if (int.TryParse(record[i], out var pk) && pk > 0)
                {
                    pevneKody.Add(pk);
                }
            }

            yield return new RawZastavka(
                CisloZastavky: id,
                NazevObce: record[1],
                CastObce: NullIfEmpty(record[2]),
                BlizsiUrceni: NullIfEmpty(record[3]),
                Okres: record[4],
                Stat: record[5],
                PevneKody: pevneKody);
        }
    }

    private static IEnumerable<RawZaslinka> ParseZaslinky(Stream stream)
    {
        foreach (var record in JdfCsvReader.ReadRecords(stream))
        {
            if (record.Count < 9 ||
                !int.TryParse(record[1], out var seq) ||
                !int.TryParse(record[3], out var stopNo))
            {
                continue;
            }

            var pevneKody = new List<int>();
            for (var i = 4; i < Math.Min(8, record.Count); i++)
            {
                if (int.TryParse(record[i], out var pk) && pk > 0)
                {
                    pevneKody.Add(pk);
                }
            }

            yield return new RawZaslinka(
                CisloLinky: record[0],
                TarifniCislo: seq,
                TarifniPasmo: NullIfEmpty(record[2]),
                CisloZastavky: stopNo,
                PevneKody: pevneKody,
                RozliseniLinky: short.TryParse(record[8], out var rl) ? rl : (short)1);
        }
    }

    private static IEnumerable<RawSpoj> ParseSpoje(Stream stream)
    {
        foreach (var record in JdfCsvReader.ReadRecords(stream))
        {
            if (record.Count < 14 || !int.TryParse(record[1], out var tripNo))
            {
                continue;
            }

            var pevneKody = new List<int>();
            for (var i = 2; i < Math.Min(12, record.Count); i++)
            {
                if (int.TryParse(record[i], out var pk) && pk > 0)
                {
                    pevneKody.Add(pk);
                }
            }

            yield return new RawSpoj(
                CisloLinky: record[0],
                CisloSpoje: tripNo,
                PevneKody: pevneKody,
                RozliseniLinky: short.TryParse(record[13], out var rl) ? rl : (short)1);
        }
    }

    private static IEnumerable<RawZasspoj> ParseZasspoje(Stream stream)
    {
        foreach (var record in JdfCsvReader.ReadRecords(stream))
        {
            if (record.Count < 15 ||
                !int.TryParse(record[1], out var tripNo) ||
                !int.TryParse(record[2], out var tarifNo) ||
                !int.TryParse(record[3], out var stopNo))
            {
                continue;
            }

            var pevneKody = new List<int>();
            for (var i = 5; i < Math.Min(8, record.Count); i++)
            {
                if (int.TryParse(record[i], out var pk) && pk > 0)
                {
                    pevneKody.Add(pk);
                }
            }

            yield return new RawZasspoj(
                CisloLinky: record[0],
                CisloSpoje: tripNo,
                TarifniCislo: tarifNo,
                CisloZastavky: stopNo,
                CisloStanoviste: NullIfEmpty(record[4]),
                PevneKody: pevneKody,
                TarifniKm: int.TryParse(record[8], out var tkm) ? tkm : null,
                SkutecneKm: int.TryParse(record[9], out var skm) ? skm : null,
                CasPrijezduMinRaw: record[10],
                CasOdjezduMinRaw: record[11],
                CasPrijezduMaxRaw: record[12],
                CasOdjezduMaxRaw: record[13],
                RozliseniLinky: short.TryParse(record[14], out var rl) ? rl : (short)1);
        }
    }

    private static IEnumerable<RawCaskod> ParseCaskody(Stream stream)
    {
        foreach (var record in JdfCsvReader.ReadRecords(stream))
        {
            if (record.Count < 9 ||
                !int.TryParse(record[1], out var tripNo) ||
                !int.TryParse(record[2], out var idx) ||
                !int.TryParse(record[4], out var type))
            {
                continue;
            }

            yield return new RawCaskod(
                CisloLinky: record[0],
                CisloSpoje: tripNo,
                IndexCaskodu: idx,
                OznaceniCaskodu: NullIfEmpty(record[3]),
                TypCaskodu: type,
                DatumOd: ParseDate(record[5]),
                DatumDo: ParseDate(record[6]),
                TextPoznamky: NullIfEmpty(record[7]),
                RozliseniLinky: short.TryParse(record[8], out var rl) ? rl : (short)1);
        }
    }

    private static IEnumerable<RawLinExt> ParseLinExt(Stream stream)
    {
        foreach (var record in JdfCsvReader.ReadRecords(stream))
        {
            if (record.Count < 7 || !int.TryParse(record[1], out var seq))
            {
                continue;
            }

            yield return new RawLinExt(
                CisloLinky: record[0],
                PoradoveCislo: seq,
                KodDopravy: record[2],
                OznaceniLinky: record[3],
                PriznakIntegrace: record[4] == "1",
                RozliseniLinky: short.TryParse(record[6], out var rl) ? rl : (short)1);
        }
    }

    private static IEnumerable<RawAltDop> ParseAltDop(Stream stream)
    {
        foreach (var record in JdfCsvReader.ReadRecords(stream))
        {
            if (record.Count < 15 || !int.TryParse(record[1], out var tripNo))
            {
                continue;
            }

            yield return new RawAltDop(
                CisloLinky: record[0],
                CisloSpoje: tripNo,
                IcDopravce: NullIfEmpty(record[2]),
                PlatnostOd: ParseDate(record[11]),
                PlatnostDo: ParseDate(record[12]),
                CisloDopravce: int.TryParse(record[13], out var cd) ? cd : 1,
                RozliseniLinky: short.TryParse(record[14], out var rl) ? rl : (short)1);
        }
    }

    private static string? NullIfEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    public static DateOnly? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 8)
        {
            return null;
        }

        if (int.TryParse(value.AsSpan(0, 2), out var day) &&
            int.TryParse(value.AsSpan(2, 2), out var month) &&
            int.TryParse(value.AsSpan(4, 4), out var year) &&
            day >= 1 && day <= 31 && month >= 1 && month <= 12 && year >= 1990 && year <= 2100)
        {
            try
            {
                return new DateOnly(year, month, day);
            }
            catch
            {
                return null;
            }
        }

        return null;
    }
}
