using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using OutlookWeeklyReport.Core.Models;

namespace OutlookWeeklyReport.Core.Services;

/// <summary>
/// Scrive detail.csv e summary.csv nella cartella di output.
/// Separatore ";", encoding UTF-8 con BOM (compatibile con Excel in locale italiano).
/// </summary>
public sealed class CsvExportService
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

        var detailPath  = Path.Combine(outputFolder, "detail.csv");
        var summaryPath = Path.Combine(outputFolder, "summary.csv");

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
        csv.WriteField("Categoria");
        csv.WriteField("Nome Evento");
        csv.WriteField("Durata (ore)");
        csv.NextRecord();

        // Righe ordinate: prima per categoria, poi per nome
        foreach (var e in events
            .OrderBy(e => e.Category ?? "\uFFFF")
            .ThenBy(e => e.Subject))
        {
            csv.WriteField(e.Category ?? string.Empty);
            csv.WriteField(e.Subject);
            csv.WriteField(Math.Round(e.DurationHours, 2).ToString("F2",
                System.Globalization.CultureInfo.InvariantCulture));
            csv.NextRecord();
        }
    }

    // ── summary.csv ───────────────────────────────────────────────────────────

    private static void WriteSummary(List<CalendarEvent> events, string path)
    {
        double total = events.Sum(e => e.DurationHours);

        // Righe categorizzate: aggrega per categoria
        var categorized = events
            .Where(e => e.Category != null)
            .GroupBy(e => e.Category!)
            .Select(g => new CategorySummary
            {
                Label      = g.Key,
                TotalHours = Math.Round(g.Sum(e => e.DurationHours), 2),
                Percentage = FormatPercent(g.Sum(e => e.DurationHours), total),
            });

        // Righe non categorizzate: una riga per evento
        var uncategorized = events
            .Where(e => e.Category == null)
            .Select(e => new CategorySummary
            {
                Label      = e.Subject,
                TotalHours = Math.Round(e.DurationHours, 2),
                Percentage = FormatPercent(e.DurationHours, total),
            });

        var rows = categorized
            .Concat(uncategorized)
            .OrderByDescending(r => r.TotalHours)
            .ToList();

        using var sw  = new StreamWriter(path, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        using var csv = new CsvWriter(sw, SemicolonConfig);

        // Intestazione
        csv.WriteField("Categoria / Evento");
        csv.WriteField("Ore totali");
        csv.WriteField("% sul totale");
        csv.NextRecord();

        foreach (var row in rows)
        {
            csv.WriteField(row.Label);
            csv.WriteField(row.TotalHours.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
            csv.WriteField(row.Percentage);
            csv.NextRecord();
        }

        // Riga TOTALE
        csv.WriteField("TOTALE");
        csv.WriteField(Math.Round(total, 2).ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
        csv.WriteField("100%");
        csv.NextRecord();
    }

    private static string FormatPercent(double value, double total)
        => total > 0 ? $"{value / total * 100:F1}%" : "0%";
}
