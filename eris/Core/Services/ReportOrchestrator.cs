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
    /// <param name="filters">Filtri di esclusione per categoria, cliente, progetto e topic.</param>
    public async Task<ReportResult> GenerateAsync(
        WeekPeriod       period,
        string           outputBaseDir,
        ExportFormat     format  = ExportFormat.Xlsx,
        EventFilters?    filters = null)
    {
        var week   = WeekRange.FromPeriod(period);
        var events = ApplyExclusions(await _source.GetEventsAsync(week), filters);

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
        WeekRange     range,
        string        outputBaseDir,
        ExportFormat  format  = ExportFormat.Xlsx,
        EventFilters? filters = null)
    {
        var events = ApplyExclusions(await _source.GetEventsAsync(range), filters);

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
        EventFilters?       filters)
    {
        if (filters is null || filters.IsEmpty)
            return events;

        var excludeTentative = filters.ExcludeTentative;
        var cats     = filters.Categories.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray();
        var clients  = filters.Clients.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray();
        var projects = filters.Projects.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray();
        var topics   = filters.Topics.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray();

        return events.Where(e =>
            (!excludeTentative || !e.IsTentative) &&
            (cats.Length     == 0 || !MatchesAnyFilter(e.Category, cats))  &&
            (clients.Length  == 0 || !MatchesAnyFilter(e.Client, clients)) &&
            (projects.Length == 0 || !MatchesAnyFilter(e.Project, projects)) &&
            (topics.Length   == 0 || !MatchesAnyFilter(e.Topic ?? e.Subject, topics))
        ).ToList();
    }

    private static bool MatchesAnyFilter(string? value, string[] filters)
    {
        if (string.IsNullOrWhiteSpace(value) || filters.Length == 0)
            return false;

        var normalizedValue = value.Trim();

        return filters.Any(filter =>
            normalizedValue.Equals(filter, StringComparison.OrdinalIgnoreCase) ||
            normalizedValue.Contains(filter, StringComparison.OrdinalIgnoreCase));
    }
}
