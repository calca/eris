using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using eris.Core.Models;

namespace eris.Core.Services;

/// <summary>
/// Scrive detail.csv e summary.csv nella cartella di output.
/// Separatore ";", encoding UTF-8 con BOM (compatibile con Excel in locale italiano).
/// </summary>
public sealed class CsvExportService : IExportService
{
    private static readonly CsvConfiguration SemicolonConfig =
        new(System.Globalization.CultureInfo.InvariantCulture)
        {
            Delimiter       = ";",
            HasHeaderRecord = false,
        };

    public (string DetailPath, string SummaryPath) Export(
        List<CalendarEvent> events,
        string outputFolder,
        WeekRange week)
    {
        Directory.CreateDirectory(outputFolder);

        var ts          = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var detailPath  = Path.Combine(outputFolder, $"{week.FolderName}_{ts}-detail.csv");
        var summaryPath = Path.Combine(outputFolder, $"{week.FolderName}_{ts}-summary.csv");

        WriteDetail(events, detailPath);
        WriteSummary(events, summaryPath);

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
            csv.WriteField(Math.Round(e.DurationHours, 2).ToString("F2",
                System.Globalization.CultureInfo.InvariantCulture));
            csv.NextRecord();
        }
    }

    // ── summary.csv ───────────────────────────────────────────────────────────

    private static void WriteSummary(List<CalendarEvent> events, string path)
    {
        double total = events.Sum(e => e.DurationHours);

        // Raggruppa ogni evento per la chiave di aggregazione:
        //   1. Category (se presente)
        //   2. Project  (se presente)
        //   3. Topic / Subject (fallback)
        var rows = events
            .GroupBy(e => new
            {
                Cat     = e.Category ?? string.Empty,
                Client  = e.Client   ?? string.Empty,
                Project = e.Project  ?? string.Empty,
                Topic   = e.Category != null ? string.Empty     // aggregato per cat → topic vuoto
                        : e.Project  != null ? string.Empty     // aggregato per progetto → topic vuoto
                        : e.Topic    ?? e.Subject,              // fallback: aggrega per topic/subject
            })
            .Select(g =>
            {
                var hours = Math.Round(g.Sum(e => e.DurationHours), 2);
                return new CategorySummary
                {
                    Category   = g.Key.Cat,
                    Client     = g.Key.Client,
                    Project    = g.Key.Project,
                    Topic      = g.Key.Topic,
                    TotalHours = hours,
                    Percentage = FormatPercent(hours, total),
                };
            })
            .OrderByDescending(r => r.TotalHours)
            .ToList();

        using var sw  = new StreamWriter(path, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        using var csv = new CsvWriter(sw, SemicolonConfig);

        // Intestazione
        csv.WriteField("Categoria");
        csv.WriteField("Cliente");
        csv.WriteField("Progetto");
        csv.WriteField("Topic");
        csv.WriteField("Ore");
        csv.WriteField("%");
        csv.NextRecord();

        foreach (var row in rows)
        {
            csv.WriteField(row.Category);
            csv.WriteField(row.Client);
            csv.WriteField(row.Project);
            csv.WriteField(row.Topic);
            csv.WriteField(row.TotalHours.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
            csv.WriteField(row.Percentage);
            csv.NextRecord();
        }

        // Riga TOTALE
        csv.WriteField(string.Empty);
        csv.WriteField(string.Empty);
        csv.WriteField(string.Empty);
        csv.WriteField("TOTALE");
        csv.WriteField(Math.Round(total, 2).ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
        csv.WriteField("100%");
        csv.NextRecord();
    }

    private static string FormatPercent(double value, double total)
        => total > 0 ? $"{value / total * 100:F1}%" : "0%";
}
