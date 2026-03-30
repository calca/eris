using ClosedXML.Excel;
using eris.Core.ExportServices;
using eris.Core.Models;

namespace eris.Core.Services;

/// <summary>
/// Scrive detail.xlsx e summary.xlsx nella cartella di output.
/// Genera un unico file .xlsx con due fogli (Detail e Summary).
/// </summary>
public sealed class XlsxExportService : IExportService
{
    private static readonly ExportMetricsCalculator MetricsCalculator = new();

    public (string DetailPath, string SummaryPath) Export(
        List<CalendarEvent> events,
        string outputFolder,
        WeekRange week,
        double weeklyHours = 40)
    {
        Directory.CreateDirectory(outputFolder);

        var ts       = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var filePath = Path.Combine(outputFolder, $"{week.FolderName}_{ts}.xlsx");
        var metrics = MetricsCalculator.Compute(events, weeklyHours);

        using var wb = new XLWorkbook();
        WriteSummaryByTag(wb, metrics);
        WriteSummary(wb, metrics);
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

    private static void WriteSummary(XLWorkbook wb, ExportMetricsSnapshot metrics)
    {
        var ws = wb.Worksheets.Add("Summary");

        // Monte ore
        ws.Cell(1, 1).Value = "Monte ore settimanale";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 2).Value = metrics.WeeklyHours;
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
        ws.Cell(3, 9).Value = "% Meeting";
        ws.Cell(3, 10).Value = "Ore Interne";
        ws.Cell(3, 11).Value = "Ore Totali";

        var headerRange = ws.Range(3, 1, 3, 11);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e293b");
        headerRange.Style.Font.FontColor = XLColor.White;

        int row = 4;
        foreach (var r in metrics.SummaryRows)
        {
            ws.Cell(row, 1).Value = r.Category;
            ws.Cell(row, 2).Value = r.Client;
            ws.Cell(row, 3).Value = r.Project;
            ws.Cell(row, 4).Value = r.Topic;
            ws.Cell(row, 5).Value = r.Tag;
            ws.Cell(row, 6).Value = r.MeetingCount;
            ws.Cell(row, 7).Value = r.TotalHours;
            ws.Cell(row, 7).Style.NumberFormat.Format = "0.00";
            ws.Cell(row, 8).Value = r.ShareOfWeeklyHours;
            ws.Cell(row, 9).Value = r.ShareOfMeetingHours;
            ws.Cell(row, 10).Value = r.InternalHours;
            ws.Cell(row, 10).Style.NumberFormat.Format = "0.00";
            ws.Cell(row, 11).Value = r.TotalSpentHours;
            ws.Cell(row, 11).Style.NumberFormat.Format = "0.00";
            row++;
        }

        // Riga TOTALE
        ws.Cell(row, 5).Value = "TOTALE";
        ws.Cell(row, 5).Style.Font.Bold = true;
        ws.Cell(row, 6).Value = metrics.Totals.MeetingCount;
        ws.Cell(row, 6).Style.Font.Bold = true;
        ws.Cell(row, 7).Value = metrics.Totals.MeetingHours;
        ws.Cell(row, 7).Style.NumberFormat.Format = "0.00";
        ws.Cell(row, 7).Style.Font.Bold = true;
        ws.Cell(row, 8).Value = metrics.Totals.ShareOfWeeklyHours;
        ws.Cell(row, 8).Style.Font.Bold = true;
        ws.Cell(row, 9).Value = metrics.Totals.ShareOfMeetingHours;
        ws.Cell(row, 9).Style.Font.Bold = true;
        ws.Cell(row, 10).Value = metrics.Totals.InternalHours;
        ws.Cell(row, 10).Style.NumberFormat.Format = "0.00";
        ws.Cell(row, 10).Style.Font.Bold = true;
        ws.Cell(row, 11).Value = metrics.Totals.TotalSpentHours;
        ws.Cell(row, 11).Style.NumberFormat.Format = "0.00";
        ws.Cell(row, 11).Style.Font.Bold = true;

        ws.Columns().AdjustToContents();
    }

    private static void WriteSummaryByTag(XLWorkbook wb, ExportMetricsSnapshot metrics)
    {
        var ws = wb.Worksheets.Add("Summary by Tag");

        ws.Cell(1, 1).Value = "Monte ore settimanale";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 2).Value = metrics.WeeklyHours;
        ws.Cell(1, 2).Style.NumberFormat.Format = "0";
        ws.Cell(1, 2).Style.Font.Bold = true;

        ws.Cell(3, 1).Value = "Tag";
        ws.Cell(3, 2).Value = "Meeting";
        ws.Cell(3, 3).Value = "Ore";
        ws.Cell(3, 4).Value = "%";
        ws.Cell(3, 5).Value = "% Meeting";
        ws.Cell(3, 6).Value = "Ore Interne";
        ws.Cell(3, 7).Value = "Ore Totali";

        var headerRange = ws.Range(3, 1, 3, 7);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e293b");
        headerRange.Style.Font.FontColor = XLColor.White;

        int row = 4;
        foreach (var r in metrics.SummaryByTagRows)
        {
            ws.Cell(row, 1).Value = r.Tag;
            ws.Cell(row, 2).Value = r.MeetingCount;
            ws.Cell(row, 3).Value = r.TotalHours;
            ws.Cell(row, 3).Style.NumberFormat.Format = "0.00";
            ws.Cell(row, 4).Value = r.ShareOfWeeklyHours;
            ws.Cell(row, 5).Value = r.ShareOfMeetingHours;
            ws.Cell(row, 6).Value = r.InternalHours;
            ws.Cell(row, 6).Style.NumberFormat.Format = "0.00";
            ws.Cell(row, 7).Value = r.TotalSpentHours;
            ws.Cell(row, 7).Style.NumberFormat.Format = "0.00";
            row++;
        }

        ws.Cell(row, 1).Value = "TOTALE";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = metrics.Totals.MeetingCount;
        ws.Cell(row, 2).Style.Font.Bold = true;
        ws.Cell(row, 3).Value = metrics.Totals.MeetingHours;
        ws.Cell(row, 3).Style.NumberFormat.Format = "0.00";
        ws.Cell(row, 3).Style.Font.Bold = true;
        ws.Cell(row, 4).Value = metrics.Totals.ShareOfWeeklyHours;
        ws.Cell(row, 4).Style.Font.Bold = true;
        ws.Cell(row, 5).Value = metrics.Totals.ShareOfMeetingHours;
        ws.Cell(row, 5).Style.Font.Bold = true;
        ws.Cell(row, 6).Value = metrics.Totals.InternalHours;
        ws.Cell(row, 6).Style.NumberFormat.Format = "0.00";
        ws.Cell(row, 6).Style.Font.Bold = true;
        ws.Cell(row, 7).Value = metrics.Totals.TotalSpentHours;
        ws.Cell(row, 7).Style.NumberFormat.Format = "0.00";
        ws.Cell(row, 7).Style.Font.Bold = true;

        ws.Columns().AdjustToContents();
    }
}
