namespace eris.Core.Services;

/// <summary>
/// Scarica un file ICS da un URL remoto con cache locale (5 minuti).
/// </summary>
public sealed class IcsDownloadService
{
    private static readonly HttpClient Http = new();

    private static readonly string CacheDir =
        Path.Combine(Path.GetTempPath(), "eris");

    private static readonly string CachePath =
        Path.Combine(CacheDir, "calendar.ics");

    /// <summary>
    /// Scarica il file ICS dall'URL e lo salva in cache locale.
    /// Riusa la cache se il file ha meno di 5 minuti.
    /// </summary>
    public async Task<string> DownloadAsync(string url)
    {
        Directory.CreateDirectory(CacheDir);

        if (File.Exists(CachePath))
        {
            var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(CachePath);
            if (age.TotalMinutes < 5)
                return CachePath;
        }

        var content = await Http.GetStringAsync(new Uri(url));
        await File.WriteAllTextAsync(CachePath, content);
        return CachePath;
    }
}
