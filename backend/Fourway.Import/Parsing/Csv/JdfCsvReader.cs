using System.Text;

namespace Fourway.Import.Parsing.Csv;

public static class JdfCsvReader
{
    private static readonly Encoding Cp1250Encoding;

    static JdfCsvReader()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Cp1250Encoding = Encoding.GetEncoding(1250);
    }

    public static Encoding Encoding => Cp1250Encoding;

    public static IEnumerable<IReadOnlyList<string>> ReadRecords(Stream stream)
    {
        using var reader = new StreamReader(stream, Cp1250Encoding, detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);
        var currentRecord = new List<string>(16);
        var fieldBuilder = new StringBuilder(64);
        var inQuotes = false;

        while (reader.ReadLine() is { } rawLine)
        {
            var line = rawLine.TrimEnd();
            if (line.Length == 0)
            {
                continue;
            }

            for (var i = 0; i < line.Length; i++)
            {
                var ch = line[i];

                if (ch == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        fieldBuilder.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }

                    continue;
                }

                if (!inQuotes && ch == ',')
                {
                    currentRecord.Add(fieldBuilder.ToString().Trim());
                    fieldBuilder.Clear();
                    continue;
                }

                if (!inQuotes && ch == ';')
                {
                    currentRecord.Add(fieldBuilder.ToString().Trim());
                    fieldBuilder.Clear();
                    yield return currentRecord.ToArray();
                    currentRecord.Clear();
                    continue;
                }

                fieldBuilder.Append(ch);
            }

            if (inQuotes)
            {
                fieldBuilder.Append('\n');
            }
        }

        if (currentRecord.Count > 0 || fieldBuilder.Length > 0)
        {
            if (fieldBuilder.Length > 0)
            {
                currentRecord.Add(fieldBuilder.ToString().Trim());
            }

            yield return currentRecord.ToArray();
        }
    }
}
