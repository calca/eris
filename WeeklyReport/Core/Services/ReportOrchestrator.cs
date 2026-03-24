using OutlookWeeklyReport.Core.Models;

namespace OutlookWeeklyReport.Core.Services;

/// <summary>Risultato della generazione di un report settimanale.</summary>
public sealed class ReportResult
{
    public int        EventCount     { get; init; }
    public double     TotalHours     { get; init; }
    public string     DetailCsvPath  { get; init; } = string.Empty;
    public string     SummaryCsvPath { get; init; } = string.Empty;
    public WeekRange  Week           { get; init; } = null!;
}

/// <summary>
/// Punto di ingresso unico per CLI e UI: autentica, recupera eventi, esporta CSV.
/// </summary>
public sealed class ReportOrchestrator
{
    private readonly GraphAuthService  _auth;
    private readonly CsvExportService  _csv = new();

    public ReportOrchestrator(GraphAuthService auth) => _auth = auth;

    /// <param name="period">Settimana corrente o precedente.</param>
    /// <param name="outputBaseDir">Cartella padre; la sottocartella viene creata automaticamente.</param>
    public async Task<ReportResult> GenerateAsync(
        WeekPeriod       period,
        string           outputBaseDir)
    {
        var token           = await _auth.GetAccessTokenAsync();
        var calendarService = new CalendarService(token);
        var week            = WeekRange.FromPeriod(period);

        var events      = await calendarService.GetAcceptedEventsAsync(week);
        var reportDir   = Path.Combine(outputBaseDir, week.FolderName);
        var (detail, summary) = _csv.Export(events, reportDir, week);

        return new ReportResult
        {
            EventCount     = events.Count,
            TotalHours     = Math.Round(events.Sum(e => e.DurationHours), 2),
            DetailCsvPath  = detail,
            SummaryCsvPath = summary,
            Week           = week,
        };
    }
}
