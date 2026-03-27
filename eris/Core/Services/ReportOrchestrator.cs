using eris.Core.Models;

namespace eris.Core.Services;

/// <summary>Risultato della generazione di un report settimanale.</summary>
public sealed class ReportResult
{
    public int        EventCount   { get; init; }
    public double     TotalHours   { get; init; }
    public string     DetailPath   { get; init; } = string.Empty;
    public string     SummaryPath  { get; init; } = string.Empty;
    public WeekRange  Week         { get; init; } = null!;
}

/// <summary>
/// Punto di ingresso unico per CLI e UI: recupera eventi dalla sorgente, esporta CSV o XLSX.
/// </summary>
public sealed class ReportOrchestrator
{
    private readonly ICalendarSource _source;

    public ReportOrchestrator(ICalendarSource source) => _source = source;

    /// <param name="period">Settimana corrente o precedente.</param>
    /// <param name="outputBaseDir">Cartella padre; la sottocartella viene creata automaticamente.</param>
    /// <param name="format">Formato di esportazione (CSV o XLSX).</param>
    /// <param name="excludedCategories">Categorie di eventi da escludere (confronto case-insensitive).</param>
    public async Task<ReportResult> GenerateAsync(
        WeekPeriod       period,
        string           outputBaseDir,
        ExportFormat     format = ExportFormat.Xlsx,
        IReadOnlyList<string>? excludedCategories = null)
    {
        var week   = WeekRange.FromPeriod(period);
        var events = ApplyExclusions(await _source.GetEventsAsync(week), excludedCategories);

        IExportService exporter = format switch
        {
            ExportFormat.Xlsx => new XlsxExportService(),
            _                => new CsvExportService(),
        };

        var reportDir = Path.Combine(outputBaseDir, week.FolderName);
        var (detail, summary) = exporter.Export(events, reportDir, week);

        return new ReportResult
        {
            EventCount  = events.Count,
            TotalHours  = Math.Round(events.Sum(e => e.DurationHours), 2),
            DetailPath  = detail,
            SummaryPath = summary,
            Week        = week,
        };
    }

    public async Task<ReportResult> GenerateAsync(
        WeekRange    range,
        string       outputBaseDir,
        ExportFormat format = ExportFormat.Xlsx,
        IReadOnlyList<string>? excludedCategories = null)
    {
        var events = ApplyExclusions(await _source.GetEventsAsync(range), excludedCategories);

        IExportService exporter = format switch
        {
            ExportFormat.Xlsx => new XlsxExportService(),
            _                => new CsvExportService(),
        };

        var reportDir = Path.Combine(outputBaseDir, range.FolderName);
        var (detail, summary) = exporter.Export(events, reportDir, range);

        return new ReportResult
        {
            EventCount  = events.Count,
            TotalHours  = Math.Round(events.Sum(e => e.DurationHours), 2),
            DetailPath  = detail,
            SummaryPath = summary,
            Week        = range,
        };
    }

    private static List<CalendarEvent> ApplyExclusions(
        List<CalendarEvent> events,
        IReadOnlyList<string>? excludedCategories)
    {
        if (excludedCategories is not { Count: > 0 })
            return events;

        var excluded = new HashSet<string>(excludedCategories, StringComparer.OrdinalIgnoreCase);
        return events.Where(e => e.Category is null || !excluded.Contains(e.Category)).ToList();
    }
}
