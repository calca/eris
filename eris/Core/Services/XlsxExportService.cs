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
        WeekRange week,
        double weeklyHours = 40)
    {
        Directory.CreateDirectory(outputFolder);

        var ts       = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var filePath = Path.Combine(outputFolder, $"{week.FolderName}_{ts}.xlsx");

        using var wb = new XLWorkbook();
        WriteSummaryByTag(wb, events, weeklyHours);
        WriteSummary(wb, events, weeklyHours);
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
        ws.Cell(1, 8).Value = "Tag";
        ws.Cell(1, 9).Value = "Ore";

        var headerRange = ws.Range(1, 1, 1, 9);
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
            ws.Cell(row, 8).Value = e.Tag ?? string.Empty;
            ws.Cell(row, 9).Value = Math.Round(e.DurationHours, 2);
            ws.Cell(row, 9).Style.NumberFormat.Format = "0.00";
            row++;
        }

        ws.Columns().AdjustToContents();
    }

    private static void WriteSummary(XLWorkbook wb, List<CalendarEvent> events, double weeklyHours)
    {
        var ws = wb.Worksheets.Add("Summary");

        double total = events.Sum(e => e.DurationHours);
        int totalMeetings = events.Count;
        double percentBase = weeklyHours > 0 ? weeklyHours : total;

        // Monte ore
        ws.Cell(1, 1).Value = "Monte ore settimanale";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 2).Value = weeklyHours;
        ws.Cell(1, 2).Style.NumberFormat.Format = "0";
        ws.Cell(1, 2).Style.Font.Bold = true;

        // Intestazione
        ws.Cell(3, 1).Value = "Categoria";
        ws.Cell(3, 2).Value = "Cliente";
        ws.Cell(3, 3).Value = "Progetto";
        ws.Cell(3, 4).Value = "Topic";
        ws.Cell(3, 5).Value = "Tag";
        ws.Cell(3, 6).Value = "Meeting";
        ws.Cell(3, 7).Value = "Ore";
        ws.Cell(3, 8).Value = "%";

        var headerRange = ws.Range(3, 1, 3, 8);
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
                Tag     = e.Tag      ?? string.Empty,
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
                    Tag        = g.Key.Tag,
                    MeetingCount = g.Count(),
                    TotalHours = hours,
                    Percentage = FormatPercent(hours, percentBase),
                };
            })
            .OrderByDescending(r => r.TotalHours)
            .ToList();

        int row = 4;
        foreach (var r in rows)
        {
            ws.Cell(row, 1).Value = r.Category;
            ws.Cell(row, 2).Value = r.Client;
            ws.Cell(row, 3).Value = r.Project;
            ws.Cell(row, 4).Value = r.Topic;
            ws.Cell(row, 5).Value = r.Tag;
            ws.Cell(row, 6).Value = r.MeetingCount;
            ws.Cell(row, 7).Value = r.TotalHours;
            ws.Cell(row, 7).Style.NumberFormat.Format = "0.00";
            ws.Cell(row, 8).Value = r.Percentage;
            row++;
        }

        // Riga TOTALE
        ws.Cell(row, 5).Value = "TOTALE";
        ws.Cell(row, 5).Style.Font.Bold = true;
        ws.Cell(row, 6).Value = totalMeetings;
        ws.Cell(row, 6).Style.Font.Bold = true;
        ws.Cell(row, 7).Value = Math.Round(total, 2);
        ws.Cell(row, 7).Style.NumberFormat.Format = "0.00";
        ws.Cell(row, 7).Style.Font.Bold = true;
        ws.Cell(row, 8).Value = FormatPercent(total, percentBase);
        ws.Cell(row, 8).Style.Font.Bold = true;

        ws.Columns().AdjustToContents();
    }

    private static void WriteSummaryByTag(XLWorkbook wb, List<CalendarEvent> events, double weeklyHours)
    {
        var ws = wb.Worksheets.Add("Summary by Tag");

        double total = events.Sum(e => e.DurationHours);
        int totalMeetings = events.Count;
        double percentBase = weeklyHours > 0 ? weeklyHours : total;

        ws.Cell(1, 1).Value = "Monte ore settimanale";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 2).Value = weeklyHours;
        ws.Cell(1, 2).Style.NumberFormat.Format = "0";
        ws.Cell(1, 2).Style.Font.Bold = true;

        ws.Cell(3, 1).Value = "Tag";
        ws.Cell(3, 2).Value = "Meeting";
        ws.Cell(3, 3).Value = "Ore";
        ws.Cell(3, 4).Value = "%";

        var headerRange = ws.Range(3, 1, 3, 4);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e293b");
        headerRange.Style.Font.FontColor = XLColor.White;

        var rows = events
            .GroupBy(e => e.Tag ?? string.Empty)
            .Select(g =>
            {
                var hours = Math.Round(g.Sum(e => e.DurationHours), 2);
                return new CategorySummary
                {
                    Tag = g.Key,
                    MeetingCount = g.Count(),
                    TotalHours = hours,
                    Percentage = FormatPercent(hours, percentBase),
                };
            })
            .OrderByDescending(r => r.TotalHours)
            .ToList();

        int row = 4;
        foreach (var r in rows)
        {
            ws.Cell(row, 1).Value = r.Tag;
            ws.Cell(row, 2).Value = r.MeetingCount;
            ws.Cell(row, 3).Value = r.TotalHours;
            ws.Cell(row, 3).Style.NumberFormat.Format = "0.00";
            ws.Cell(row, 4).Value = r.Percentage;
            row++;
        }

        ws.Cell(row, 1).Value = "TOTALE";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = totalMeetings;
        ws.Cell(row, 2).Style.Font.Bold = true;
        ws.Cell(row, 3).Value = Math.Round(total, 2);
        ws.Cell(row, 3).Style.NumberFormat.Format = "0.00";
        ws.Cell(row, 3).Style.Font.Bold = true;
        ws.Cell(row, 4).Value = FormatPercent(total, percentBase);
        ws.Cell(row, 4).Style.Font.Bold = true;

        ws.Columns().AdjustToContents();
    }

    private static string FormatPercent(double value, double total)
        => total > 0 ? $"{value / total * 100:F1}%" : "0%";
}
