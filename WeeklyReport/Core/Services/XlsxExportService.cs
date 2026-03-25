using ClosedXML.Excel;
using eris.Core.Models;

namespace eris.Core.Services;

/// <summary>
/// Scrive detail.xlsx e summary.xlsx nella cartella di output.
/// Genera un unico file .xlsx con due fogli (Detail e Summary).
/// </summary>
public sealed class XlsxExportService : IExportService
{
    public (string DetailPath, string SummaryPath) Export(
        List<CalendarEvent> events,
        string outputFolder,
        WeekRange week)
    {
        Directory.CreateDirectory(outputFolder);

        var ts       = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var filePath = Path.Combine(outputFolder, $"{week.FolderName}_{ts}.xlsx");

        using var wb = new XLWorkbook();
        WriteSummary(wb, events);
        WriteDetail(wb, events);
        wb.SaveAs(filePath);

        return (filePath, filePath);
    }

    private static void WriteDetail(XLWorkbook wb, List<CalendarEvent> events)
    {
        var ws = wb.Worksheets.Add("Detail");

        // Intestazione
        ws.Cell(1, 1).Value = "Data";
        ws.Cell(1, 2).Value = "Inizio";
        ws.Cell(1, 3).Value = "Fine";
        ws.Cell(1, 4).Value = "Categoria";
        ws.Cell(1, 5).Value = "Cliente";
        ws.Cell(1, 6).Value = "Progetto";
        ws.Cell(1, 7).Value = "Topic";
        ws.Cell(1, 8).Value = "Ore";

        var headerRange = ws.Range(1, 1, 1, 8);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e293b");
        headerRange.Style.Font.FontColor = XLColor.White;

        int row = 2;
        foreach (var e in events
            .OrderBy(e => e.StartTime)
            .ThenBy(e => e.Category ?? "\uFFFF")
            .ThenBy(e => e.Client ?? "\uFFFF")
            .ThenBy(e => e.Project ?? "\uFFFF")
            .ThenBy(e => e.Topic ?? e.Subject))
        {
            ws.Cell(row, 1).Value = e.StartTime?.ToString("dd/MM/yyyy") ?? string.Empty;
            ws.Cell(row, 2).Value = e.StartTime?.ToString("HH:mm") ?? string.Empty;
            ws.Cell(row, 3).Value = e.EndTime?.ToString("HH:mm") ?? string.Empty;
            ws.Cell(row, 4).Value = e.Category ?? string.Empty;
            ws.Cell(row, 5).Value = e.Client ?? string.Empty;
            ws.Cell(row, 6).Value = e.Project ?? string.Empty;
            ws.Cell(row, 7).Value = e.Topic ?? e.Subject;
            ws.Cell(row, 8).Value = Math.Round(e.DurationHours, 2);
            ws.Cell(row, 8).Style.NumberFormat.Format = "0.00";
            row++;
        }

        ws.Columns().AdjustToContents();
    }

    private static void WriteSummary(XLWorkbook wb, List<CalendarEvent> events)
    {
        var ws = wb.Worksheets.Add("Summary");

        double total = events.Sum(e => e.DurationHours);

        // Intestazione
        ws.Cell(1, 1).Value = "Categoria";
        ws.Cell(1, 2).Value = "Cliente";
        ws.Cell(1, 3).Value = "Progetto";
        ws.Cell(1, 4).Value = "Topic";
        ws.Cell(1, 5).Value = "Ore";
        ws.Cell(1, 6).Value = "%";

        var headerRange = ws.Range(1, 1, 1, 6);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e293b");
        headerRange.Style.Font.FontColor = XLColor.White;

        var rows = events
            .GroupBy(e => new
            {
                Cat     = e.Category ?? string.Empty,
                Client  = e.Client   ?? string.Empty,
                Project = e.Project  ?? string.Empty,
                Topic   = e.Category != null ? string.Empty
                        : e.Project  != null ? string.Empty
                        : e.Topic    ?? e.Subject,
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

        int row = 2;
        foreach (var r in rows)
        {
            ws.Cell(row, 1).Value = r.Category;
            ws.Cell(row, 2).Value = r.Client;
            ws.Cell(row, 3).Value = r.Project;
            ws.Cell(row, 4).Value = r.Topic;
            ws.Cell(row, 5).Value = r.TotalHours;
            ws.Cell(row, 5).Style.NumberFormat.Format = "0.00";
            ws.Cell(row, 6).Value = r.Percentage;
            row++;
        }

        // Riga TOTALE
        ws.Cell(row, 4).Value = "TOTALE";
        ws.Cell(row, 4).Style.Font.Bold = true;
        ws.Cell(row, 5).Value = Math.Round(total, 2);
        ws.Cell(row, 5).Style.NumberFormat.Format = "0.00";
        ws.Cell(row, 5).Style.Font.Bold = true;
        ws.Cell(row, 6).Value = "100%";
        ws.Cell(row, 6).Style.Font.Bold = true;

        ws.Columns().AdjustToContents();
    }

    private static string FormatPercent(double value, double total)
        => total > 0 ? $"{value / total * 100:F1}%" : "0%";
}
