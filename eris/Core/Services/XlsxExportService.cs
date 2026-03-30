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
        var localization = ExportLocalization.Current();
        var metrics = MetricsCalculator.Compute(events, weeklyHours, localization.FormatCulture);

        using var wb = new XLWorkbook();
        WriteSummaryByTag(wb, metrics, localization);
        WriteSummary(wb, metrics, localization);
        WriteDetail(wb, events, localization);
        wb.SaveAs(filePath);

        return (filePath, filePath);
    }

    private static void WriteDetail(XLWorkbook wb, List<CalendarEvent> events, ExportLocalization localization)
    {
        var ws = wb.Worksheets.Add(localization.Get("DetailSheetName"));

        // Intestazione
        ws.Cell(1, 1).Value = localization.Get("Date");
        ws.Cell(1, 2).Value = localization.Get("Start");
        ws.Cell(1, 3).Value = localization.Get("End");
        ws.Cell(1, 4).Value = localization.Get("Category");
        ws.Cell(1, 5).Value = localization.Get("Client");
        ws.Cell(1, 6).Value = localization.Get("Project");
        ws.Cell(1, 7).Value = localization.Get("Topic");
        ws.Cell(1, 8).Value = localization.Get("Tag");
        ws.Cell(1, 9).Value = localization.Get("Hours");

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
            ws.Cell(row, 1).Value = e.StartTime?.ToString("d", localization.FormatCulture) ?? string.Empty;
            ws.Cell(row, 2).Value = e.StartTime?.ToString("t", localization.FormatCulture) ?? string.Empty;
            ws.Cell(row, 3).Value = e.EndTime?.ToString("t", localization.FormatCulture) ?? string.Empty;
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

    private static void WriteSummary(XLWorkbook wb, ExportMetricsSnapshot metrics, ExportLocalization localization)
    {
        var ws = wb.Worksheets.Add(localization.Get("SummarySheetName"));
        const int dataStartRow = 4;

        // Monte ore
        ws.Cell(1, 1).Value = localization.Get("WeeklyHours");
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 2).Value = metrics.WeeklyHours;
        ws.Cell(1, 2).Style.NumberFormat.Format = "0";
        ws.Cell(1, 2).Style.Font.Bold = true;

        // Intestazione
        ws.Cell(3, 1).Value = localization.Get("Category");
        ws.Cell(3, 2).Value = localization.Get("Client");
        ws.Cell(3, 3).Value = localization.Get("Project");
        ws.Cell(3, 4).Value = localization.Get("Topic");
        ws.Cell(3, 5).Value = localization.Get("Tag");
        ws.Cell(3, 6).Value = localization.Get("Meetings");
        ws.Cell(3, 7).Value = localization.Get("Hours");
        ws.Cell(3, 8).Value = localization.Get("Percent");
        ws.Cell(3, 9).Value = localization.Get("MeetingPercent");
        ws.Cell(3, 10).Value = localization.Get("InternalHours");
        ws.Cell(3, 11).Value = localization.Get("TotalHours");

        var headerRange = ws.Range(3, 1, 3, 11);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e293b");
        headerRange.Style.Font.FontColor = XLColor.White;

        int row = dataStartRow;
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
            ws.Cell(row, 8).FormulaA1 = $"IF($B$1>0,G{row}/$B$1,IFERROR(G{row}/SUM($G${dataStartRow}:$G${row - 1}),0))";
            ws.Cell(row, 8).Style.NumberFormat.Format = "0.0%";
            ws.Cell(row, 9).FormulaA1 = $"IFERROR(G{row}/SUM($G${dataStartRow}:$G${row - 1}),0)";
            ws.Cell(row, 9).Style.NumberFormat.Format = "0.0%";
            ws.Cell(row, 10).FormulaA1 = $"MAX($B$1-SUM($G${dataStartRow}:$G${row - 1}),0)*IFERROR(G{row}/SUM($G${dataStartRow}:$G${row - 1}),0)";
            ws.Cell(row, 10).Style.NumberFormat.Format = "0.00";
            ws.Cell(row, 11).FormulaA1 = $"G{row}+J{row}";
            ws.Cell(row, 11).Style.NumberFormat.Format = "0.00";
            row++;
        }

        var dataEndRow = row - 1;
        var hasDataRows = dataEndRow >= dataStartRow;

        if (hasDataRows)
            ws.Range(3, 1, dataEndRow, 11).SetAutoFilter();
        else
            ws.Range(3, 1, 3, 11).SetAutoFilter();

        // Riga TOTALE
        ws.Cell(row, 5).Value = localization.Get("Total");
        ws.Cell(row, 5).Style.Font.Bold = true;
        ws.Cell(row, 6).FormulaA1 = hasDataRows ? $"SUBTOTAL(109,$F${dataStartRow}:$F${dataEndRow})" : "0";
        ws.Cell(row, 6).Style.Font.Bold = true;
        ws.Cell(row, 7).FormulaA1 = hasDataRows ? $"SUBTOTAL(109,$G${dataStartRow}:$G${dataEndRow})" : "0";
        ws.Cell(row, 7).Style.NumberFormat.Format = "0.00";
        ws.Cell(row, 7).Style.Font.Bold = true;
        ws.Cell(row, 8).FormulaA1 = hasDataRows
            ? $"IF($B$1>0,G{row}/$B$1,IFERROR(G{row}/SUBTOTAL(109,$G${dataStartRow}:$G${dataEndRow}),0))"
            : "0";
        ws.Cell(row, 8).Style.NumberFormat.Format = "0.0%";
        ws.Cell(row, 8).Style.Font.Bold = true;
        ws.Cell(row, 9).FormulaA1 = hasDataRows ? $"IFERROR(G{row}/SUBTOTAL(109,$G${dataStartRow}:$G${dataEndRow}),0)" : "0";
        ws.Cell(row, 9).Style.NumberFormat.Format = "0.0%";
        ws.Cell(row, 9).Style.Font.Bold = true;
        ws.Cell(row, 10).FormulaA1 = hasDataRows ? $"MAX($B$1-SUBTOTAL(109,$G${dataStartRow}:$G${dataEndRow}),0)" : "MAX($B$1,0)";
        ws.Cell(row, 10).Style.NumberFormat.Format = "0.00";
        ws.Cell(row, 10).Style.Font.Bold = true;
        ws.Cell(row, 11).FormulaA1 = $"G{row}+J{row}";
        ws.Cell(row, 11).Style.NumberFormat.Format = "0.00";
        ws.Cell(row, 11).Style.Font.Bold = true;

        ws.Columns().AdjustToContents();
    }

    private static void WriteSummaryByTag(XLWorkbook wb, ExportMetricsSnapshot metrics, ExportLocalization localization)
    {
        var ws = wb.Worksheets.Add(localization.Get("SummaryByTagSheetName"));
        const int dataStartRow = 4;

        ws.Cell(1, 1).Value = localization.Get("WeeklyHours");
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 2).Value = metrics.WeeklyHours;
        ws.Cell(1, 2).Style.NumberFormat.Format = "0";
        ws.Cell(1, 2).Style.Font.Bold = true;

        ws.Cell(3, 1).Value = localization.Get("Tag");
        ws.Cell(3, 2).Value = localization.Get("Meetings");
        ws.Cell(3, 3).Value = localization.Get("Hours");
        ws.Cell(3, 4).Value = localization.Get("Percent");
        ws.Cell(3, 5).Value = localization.Get("MeetingPercent");
        ws.Cell(3, 6).Value = localization.Get("InternalHours");
        ws.Cell(3, 7).Value = localization.Get("TotalHours");

        var headerRange = ws.Range(3, 1, 3, 7);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e293b");
        headerRange.Style.Font.FontColor = XLColor.White;

        int row = dataStartRow;
        foreach (var r in metrics.SummaryByTagRows)
        {
            ws.Cell(row, 1).Value = r.Tag;
            ws.Cell(row, 2).Value = r.MeetingCount;
            ws.Cell(row, 3).Value = r.TotalHours;
            ws.Cell(row, 3).Style.NumberFormat.Format = "0.00";
            ws.Cell(row, 4).FormulaA1 = $"IF($B$1>0,C{row}/$B$1,IFERROR(C{row}/SUM($C${dataStartRow}:$C${row - 1}),0))";
            ws.Cell(row, 4).Style.NumberFormat.Format = "0.0%";
            ws.Cell(row, 5).FormulaA1 = $"IFERROR(C{row}/SUM($C${dataStartRow}:$C${row - 1}),0)";
            ws.Cell(row, 5).Style.NumberFormat.Format = "0.0%";
            ws.Cell(row, 6).FormulaA1 = $"MAX($B$1-SUM($C${dataStartRow}:$C${row - 1}),0)*IFERROR(C{row}/SUM($C${dataStartRow}:$C${row - 1}),0)";
            ws.Cell(row, 6).Style.NumberFormat.Format = "0.00";
            ws.Cell(row, 7).FormulaA1 = $"C{row}+F{row}";
            ws.Cell(row, 7).Style.NumberFormat.Format = "0.00";
            row++;
        }

        var dataEndRow = row - 1;
        var hasDataRows = dataEndRow >= dataStartRow;

        if (hasDataRows)
            ws.Range(3, 1, dataEndRow, 7).SetAutoFilter();
        else
            ws.Range(3, 1, 3, 7).SetAutoFilter();

        ws.Cell(row, 1).Value = localization.Get("Total");
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).FormulaA1 = hasDataRows ? $"SUBTOTAL(109,$B${dataStartRow}:$B${dataEndRow})" : "0";
        ws.Cell(row, 2).Style.Font.Bold = true;
        ws.Cell(row, 3).FormulaA1 = hasDataRows ? $"SUBTOTAL(109,$C${dataStartRow}:$C${dataEndRow})" : "0";
        ws.Cell(row, 3).Style.NumberFormat.Format = "0.00";
        ws.Cell(row, 3).Style.Font.Bold = true;
        ws.Cell(row, 4).FormulaA1 = hasDataRows
            ? $"IF($B$1>0,C{row}/$B$1,IFERROR(C{row}/SUBTOTAL(109,$C${dataStartRow}:$C${dataEndRow}),0))"
            : "0";
        ws.Cell(row, 4).Style.NumberFormat.Format = "0.0%";
        ws.Cell(row, 4).Style.Font.Bold = true;
        ws.Cell(row, 5).FormulaA1 = hasDataRows ? $"IFERROR(C{row}/SUBTOTAL(109,$C${dataStartRow}:$C${dataEndRow}),0)" : "0";
        ws.Cell(row, 5).Style.NumberFormat.Format = "0.0%";
        ws.Cell(row, 5).Style.Font.Bold = true;
        ws.Cell(row, 6).FormulaA1 = hasDataRows ? $"MAX($B$1-SUBTOTAL(109,$C${dataStartRow}:$C${dataEndRow}),0)" : "MAX($B$1,0)";
        ws.Cell(row, 6).Style.NumberFormat.Format = "0.00";
        ws.Cell(row, 6).Style.Font.Bold = true;
        ws.Cell(row, 7).FormulaA1 = $"C{row}+F{row}";
        ws.Cell(row, 7).Style.NumberFormat.Format = "0.00";
        ws.Cell(row, 7).Style.Font.Bold = true;

        ws.Columns().AdjustToContents();
    }
}
