using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using eris.Core.ExportServices;
using eris.Core.Models;

namespace eris.Core.Services;

/// <summary>
/// Scrive detail.csv e summary.csv nella cartella di output.
/// Separatore ";", encoding UTF-8 con BOM (compatibile con Excel in locale italiano).
/// </summary>
public sealed class CsvExportService : IExportService
{
    private static readonly ExportMetricsCalculator MetricsCalculator = new();

    public (string DetailPath, string SummaryPath) Export(
        List<CalendarEvent> events,
        string outputFolder,
        WeekRange week,
        double weeklyHours = 40)
    {
        Directory.CreateDirectory(outputFolder);

        var ts          = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var detailPath  = Path.Combine(outputFolder, $"{week.FolderName}_{ts}-detail.csv");
        var summaryPath = Path.Combine(outputFolder, $"{week.FolderName}_{ts}-summary.csv");
        var summaryByTagPath = Path.Combine(outputFolder, $"{week.FolderName}_{ts}-summary-by-tag.csv");
        var localization = ExportLocalization.Current();
        var metrics = MetricsCalculator.Compute(events, weeklyHours, localization.FormatCulture);
        var csvConfig = CreateConfiguration(localization);

        WriteDetail(events, detailPath, csvConfig, localization);
        WriteSummary(summaryPath, csvConfig, metrics, localization);
        WriteSummaryByTag(summaryByTagPath, csvConfig, metrics, localization);

        return (detailPath, summaryPath);
    }

    // ── detail.csv ────────────────────────────────────────────────────────────

    private static CsvConfiguration CreateConfiguration(ExportLocalization localization)
        => new(localization.FormatCulture)
        {
            Delimiter = localization.CsvDelimiter,
            HasHeaderRecord = false,
        };

    private static void WriteDetail(
        List<CalendarEvent> events,
        string path,
        CsvConfiguration config,
        ExportLocalization localization)
    {
        using var sw  = new StreamWriter(path, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        using var csv = new CsvWriter(sw, config);

        // Intestazione
        csv.WriteField(localization.Get("Date"));
        csv.WriteField(localization.Get("Start"));
        csv.WriteField(localization.Get("End"));
        csv.WriteField(localization.Get("Category"));
        csv.WriteField(localization.Get("Client"));
        csv.WriteField(localization.Get("Project"));
        csv.WriteField(localization.Get("Topic"));
        csv.WriteField(localization.Get("Tag"));
        csv.WriteField(localization.Get("Hours"));
        csv.NextRecord();

        foreach (var e in events
            .OrderBy(e => e.StartTime)
            .ThenBy(e => e.Category ?? "\uFFFF")
            .ThenBy(e => e.Client ?? "\uFFFF")
            .ThenBy(e => e.Project ?? "\uFFFF")
            .ThenBy(e => e.Topic ?? e.Subject))
        {
            csv.WriteField(e.StartTime?.ToString("d", localization.FormatCulture) ?? string.Empty);
            csv.WriteField(e.StartTime?.ToString("t", localization.FormatCulture) ?? string.Empty);
            csv.WriteField(e.EndTime?.ToString("t", localization.FormatCulture) ?? string.Empty);
            csv.WriteField(e.Category ?? string.Empty);
            csv.WriteField(e.Client ?? string.Empty);
            csv.WriteField(e.Project ?? string.Empty);
            csv.WriteField(e.Topic ?? e.Subject);
            csv.WriteField(e.Tag ?? string.Empty);
            csv.WriteField(Math.Round(e.DurationHours, 2).ToString("F2", localization.FormatCulture));
            csv.NextRecord();
        }
    }

    // ── summary.csv ───────────────────────────────────────────────────────────

    private static void WriteSummary(
        string path,
        CsvConfiguration config,
        ExportMetricsSnapshot metrics,
        ExportLocalization localization)
    {
        using var sw  = new StreamWriter(path, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        using var csv = new CsvWriter(sw, config);

        // Monte ore
        csv.WriteField(localization.Get("WeeklyHours"));
        csv.WriteField(metrics.WeeklyHours.ToString("F0", localization.FormatCulture));
        csv.NextRecord();
        csv.NextRecord();

        // Intestazione
        csv.WriteField(localization.Get("Category"));
        csv.WriteField(localization.Get("Client"));
        csv.WriteField(localization.Get("Project"));
        csv.WriteField(localization.Get("Topic"));
        csv.WriteField(localization.Get("Tag"));
        csv.WriteField(localization.Get("Meetings"));
        csv.WriteField(localization.Get("Hours"));
        csv.WriteField(localization.Get("Percent"));
        csv.WriteField(localization.Get("MeetingPercent"));
        csv.WriteField(localization.Get("InternalHours"));
        csv.WriteField(localization.Get("TotalHours"));
        csv.NextRecord();

        foreach (var row in metrics.SummaryRows)
        {
            csv.WriteField(row.Category);
            csv.WriteField(row.Client);
            csv.WriteField(row.Project);
            csv.WriteField(row.Topic);
            csv.WriteField(row.Tag);
            csv.WriteField(row.MeetingCount);
            csv.WriteField(row.TotalHours.ToString("F2", localization.FormatCulture));
            csv.WriteField(row.ShareOfWeeklyHours);
            csv.WriteField(row.ShareOfMeetingHours);
            csv.WriteField(row.InternalHours.ToString("F2", localization.FormatCulture));
            csv.WriteField(row.TotalSpentHours.ToString("F2", localization.FormatCulture));
            csv.NextRecord();
        }

        // Riga TOTALE
        csv.WriteField(string.Empty);
        csv.WriteField(string.Empty);
        csv.WriteField(string.Empty);
        csv.WriteField(string.Empty);
        csv.WriteField(localization.Get("Total"));
        csv.WriteField(metrics.Totals.MeetingCount);
        csv.WriteField(metrics.Totals.MeetingHours.ToString("F2", localization.FormatCulture));
        csv.WriteField(metrics.Totals.ShareOfWeeklyHours);
        csv.WriteField(metrics.Totals.ShareOfMeetingHours);
        csv.WriteField(metrics.Totals.InternalHours.ToString("F2", localization.FormatCulture));
        csv.WriteField(metrics.Totals.TotalSpentHours.ToString("F2", localization.FormatCulture));
        csv.NextRecord();
    }

    private static void WriteSummaryByTag(
        string path,
        CsvConfiguration config,
        ExportMetricsSnapshot metrics,
        ExportLocalization localization)
    {
        using var sw  = new StreamWriter(path, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        using var csv = new CsvWriter(sw, config);

        csv.WriteField(localization.Get("WeeklyHours"));
        csv.WriteField(metrics.WeeklyHours.ToString("F0", localization.FormatCulture));
        csv.NextRecord();
        csv.NextRecord();

        csv.WriteField(localization.Get("Tag"));
        csv.WriteField(localization.Get("Meetings"));
        csv.WriteField(localization.Get("Hours"));
        csv.WriteField(localization.Get("Percent"));
        csv.WriteField(localization.Get("MeetingPercent"));
        csv.WriteField(localization.Get("InternalHours"));
        csv.WriteField(localization.Get("TotalHours"));
        csv.NextRecord();

        foreach (var row in metrics.SummaryByTagRows)
        {
            csv.WriteField(row.Tag);
            csv.WriteField(row.MeetingCount);
            csv.WriteField(row.TotalHours.ToString("F2", localization.FormatCulture));
            csv.WriteField(row.ShareOfWeeklyHours);
            csv.WriteField(row.ShareOfMeetingHours);
            csv.WriteField(row.InternalHours.ToString("F2", localization.FormatCulture));
            csv.WriteField(row.TotalSpentHours.ToString("F2", localization.FormatCulture));
            csv.NextRecord();
        }

        csv.WriteField(localization.Get("Total"));
        csv.WriteField(metrics.Totals.MeetingCount);
        csv.WriteField(metrics.Totals.MeetingHours.ToString("F2", localization.FormatCulture));
        csv.WriteField(metrics.Totals.ShareOfWeeklyHours);
        csv.WriteField(metrics.Totals.ShareOfMeetingHours);
        csv.WriteField(metrics.Totals.InternalHours.ToString("F2", localization.FormatCulture));
        csv.WriteField(metrics.Totals.TotalSpentHours.ToString("F2", localization.FormatCulture));
        csv.NextRecord();
    }
}
