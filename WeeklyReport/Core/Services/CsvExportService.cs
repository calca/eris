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
        csv.WriteField("Cliente");
        csv.WriteField("Progetto");
        csv.WriteField("Topic");
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
            csv.WriteField(e.Client ?? string.Empty);
            csv.WriteField(e.Project ?? string.Empty);
            csv.WriteField(e.Topic ?? string.Empty);
            csv.NextRecord();
        }
    }

    // ── summary.csv ───────────────────────────────────────────────────────────

    private static void WriteSummary(List<CalendarEvent> events, string path)
    {
        double total = events.Sum(e => e.DurationHours);

        var rows = new List<CategorySummary>();

        // Raggruppamento per Client > Project (eventi strutturati)
        var structured = events.Where(e => e.Client != null).ToList();
        if (structured.Count > 0)
        {
            var byClient = structured
                .GroupBy(e => e.Client!)
                .OrderByDescending(g => g.Sum(e => e.DurationHours));

            foreach (var clientGroup in byClient)
            {
                // Riga cliente
                var clientHours = Math.Round(clientGroup.Sum(e => e.DurationHours), 2);
                rows.Add(new CategorySummary
                {
                    Label      = clientGroup.Key,
                    TotalHours = clientHours,
                    Percentage = FormatPercent(clientHours, total),
                });

                // Sub-righe progetto
                foreach (var projGroup in clientGroup
                    .GroupBy(e => e.Project ?? "(nessun progetto)")
                    .OrderByDescending(g => g.Sum(e => e.DurationHours)))
                {
                    var projHours = Math.Round(projGroup.Sum(e => e.DurationHours), 2);
                    rows.Add(new CategorySummary
                    {
                        Label      = $"  {clientGroup.Key} > {projGroup.Key}",
                        TotalHours = projHours,
                        Percentage = FormatPercent(projHours, total),
                    });

                    // Sub-sub-righe topic
                    foreach (var topicGroup in projGroup
                        .Where(e => e.Topic != null)
                        .GroupBy(e => e.Topic!)
                        .OrderByDescending(g => g.Sum(e => e.DurationHours)))
                    {
                        var topicHours = Math.Round(topicGroup.Sum(e => e.DurationHours), 2);
                        rows.Add(new CategorySummary
                        {
                            Label      = $"    {clientGroup.Key} > {projGroup.Key} > {topicGroup.Key}",
                            TotalHours = topicHours,
                            Percentage = FormatPercent(topicHours, total),
                        });
                    }
                }
            }
        }

        // Fallback: raggruppamento per categoria (eventi non strutturati)
        var unstructured = events.Where(e => e.Client == null).ToList();

        var categorized = unstructured
            .Where(e => e.Category != null)
            .GroupBy(e => e.Category!)
            .Select(g => new CategorySummary
            {
                Label      = g.Key,
                TotalHours = Math.Round(g.Sum(e => e.DurationHours), 2),
                Percentage = FormatPercent(g.Sum(e => e.DurationHours), total),
            });

        var uncategorized = unstructured
            .Where(e => e.Category == null)
            .Select(e => new CategorySummary
            {
                Label      = e.Subject,
                TotalHours = Math.Round(e.DurationHours, 2),
                Percentage = FormatPercent(e.DurationHours, total),
            });

        rows.AddRange(categorized.Concat(uncategorized).OrderByDescending(r => r.TotalHours));

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
