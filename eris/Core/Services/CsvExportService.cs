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

    private static readonly CsvConfiguration SemicolonConfig =
        new(System.Globalization.CultureInfo.InvariantCulture)
        {
            Delimiter       = ";",
            HasHeaderRecord = false,
        };

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
        var metrics = MetricsCalculator.Compute(events, weeklyHours);

        WriteDetail(events, detailPath);
        WriteSummary(summaryPath, metrics);
        WriteSummaryByTag(summaryByTagPath, metrics);

        return (detailPath, summaryPath);
    }

    // ── detail.csv ────────────────────────────────────────────────────────────

    private static void WriteDetail(List<CalendarEvent> events, string path)
    {
        using var sw  = new StreamWriter(path, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        using var csv = new CsvWriter(sw, SemicolonConfig);

        // Intestazione
        csv.WriteField("Data");
        csv.WriteField("Inizio");
        csv.WriteField("Fine");
        csv.WriteField("Categoria");
        csv.WriteField("Cliente");
        csv.WriteField("Progetto");
        csv.WriteField("Topic");
        csv.WriteField("Tag");
        csv.WriteField("Ore");
        csv.NextRecord();

        foreach (var e in events
            .OrderBy(e => e.StartTime)
            .ThenBy(e => e.Category ?? "\uFFFF")
            .ThenBy(e => e.Client ?? "\uFFFF")
            .ThenBy(e => e.Project ?? "\uFFFF")
            .ThenBy(e => e.Topic ?? e.Subject))
        {
            csv.WriteField(e.StartTime?.ToString("dd/MM/yyyy") ?? string.Empty);
            csv.WriteField(e.StartTime?.ToString("HH:mm") ?? string.Empty);
            csv.WriteField(e.EndTime?.ToString("HH:mm") ?? string.Empty);
            csv.WriteField(e.Category ?? string.Empty);
            csv.WriteField(e.Client ?? string.Empty);
            csv.WriteField(e.Project ?? string.Empty);
            csv.WriteField(e.Topic ?? e.Subject);
            csv.WriteField(e.Tag ?? string.Empty);
            csv.WriteField(Math.Round(e.DurationHours, 2).ToString("F2",
                System.Globalization.CultureInfo.InvariantCulture));
            csv.NextRecord();
        }
    }

    // ── summary.csv ───────────────────────────────────────────────────────────

    private static void WriteSummary(string path, ExportMetricsSnapshot metrics)
    {
        using var sw  = new StreamWriter(path, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        using var csv = new CsvWriter(sw, SemicolonConfig);

        // Monte ore
        csv.WriteField("Monte ore settimanale");
        csv.WriteField(metrics.WeeklyHours.ToString("F0", System.Globalization.CultureInfo.InvariantCulture));
        csv.NextRecord();
        csv.NextRecord();

        // Intestazione
        csv.WriteField("Categoria");
        csv.WriteField("Cliente");
        csv.WriteField("Progetto");
        csv.WriteField("Topic");
        csv.WriteField("Tag");
        csv.WriteField("Meeting");
        csv.WriteField("Ore");
        csv.WriteField("%");
        csv.WriteField("% Meeting");
        csv.WriteField("Ore Interne");
        csv.WriteField("Ore Totali");
        csv.NextRecord();

        foreach (var row in metrics.SummaryRows)
        {
            csv.WriteField(row.Category);
            csv.WriteField(row.Client);
            csv.WriteField(row.Project);
            csv.WriteField(row.Topic);
            csv.WriteField(row.Tag);
            csv.WriteField(row.MeetingCount);
            csv.WriteField(row.TotalHours.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
            csv.WriteField(row.ShareOfWeeklyHours);
            csv.WriteField(row.ShareOfMeetingHours);
            csv.WriteField(row.InternalHours.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
            csv.WriteField(row.TotalSpentHours.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
            csv.NextRecord();
        }

        // Riga TOTALE
        csv.WriteField(string.Empty);
        csv.WriteField(string.Empty);
        csv.WriteField(string.Empty);
        csv.WriteField(string.Empty);
        csv.WriteField("TOTALE");
        csv.WriteField(metrics.Totals.MeetingCount);
        csv.WriteField(metrics.Totals.MeetingHours.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
        csv.WriteField(metrics.Totals.ShareOfWeeklyHours);
        csv.WriteField(metrics.Totals.ShareOfMeetingHours);
        csv.WriteField(metrics.Totals.InternalHours.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
        csv.WriteField(metrics.Totals.TotalSpentHours.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
        csv.NextRecord();
    }

    private static void WriteSummaryByTag(string path, ExportMetricsSnapshot metrics)
    {
        using var sw  = new StreamWriter(path, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        using var csv = new CsvWriter(sw, SemicolonConfig);

        csv.WriteField("Monte ore settimanale");
        csv.WriteField(metrics.WeeklyHours.ToString("F0", System.Globalization.CultureInfo.InvariantCulture));
        csv.NextRecord();
        csv.NextRecord();

        csv.WriteField("Tag");
        csv.WriteField("Meeting");
        csv.WriteField("Ore");
        csv.WriteField("%");
        csv.WriteField("% Meeting");
        csv.WriteField("Ore Interne");
        csv.WriteField("Ore Totali");
        csv.NextRecord();

        foreach (var row in metrics.SummaryByTagRows)
        {
            csv.WriteField(row.Tag);
            csv.WriteField(row.MeetingCount);
            csv.WriteField(row.TotalHours.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
            csv.WriteField(row.ShareOfWeeklyHours);
            csv.WriteField(row.ShareOfMeetingHours);
            csv.WriteField(row.InternalHours.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
            csv.WriteField(row.TotalSpentHours.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
            csv.NextRecord();
        }

        csv.WriteField("TOTALE");
        csv.WriteField(metrics.Totals.MeetingCount);
        csv.WriteField(metrics.Totals.MeetingHours.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
        csv.WriteField(metrics.Totals.ShareOfWeeklyHours);
        csv.WriteField(metrics.Totals.ShareOfMeetingHours);
        csv.WriteField(metrics.Totals.InternalHours.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
        csv.WriteField(metrics.Totals.TotalSpentHours.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
        csv.NextRecord();
    }
}
