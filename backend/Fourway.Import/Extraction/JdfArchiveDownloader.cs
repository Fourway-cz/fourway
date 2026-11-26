namespace Fourway.Import.Extraction;

public sealed class JdfArchiveDownloader
{
    private readonly HttpClient _httpClient;

    public JdfArchiveDownloader(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(30)
        };
    }

    public async Task<string> DownloadAsync(string url, string destinationPath, CancellationToken ct = default)
    {
        Console.WriteLine($"Stahuji {url}...");

        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
        await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
        await using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);

        var buffer = new byte[81920];
        long totalRead = 0;
        int read;
        var lastReported = 0;

        while ((read = await contentStream.ReadAsync(buffer, ct)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, read), ct);
            totalRead += read;

            if (totalBytes > 0)
            {
                var percentage = (int)((totalRead * 100) / totalBytes);
                if (percentage >= lastReported + 10)
                {
                    lastReported = percentage;
                    Console.WriteLine($"Staženo: {percentage} % ({totalRead / (1024 * 1024)} MB / {totalBytes / (1024 * 1024)} MB)");
                }
            }
        }

        Console.WriteLine($"Stažení dokončeno ({totalRead / (1024 * 1024)} MB).");
        return destinationPath;
    }
}
