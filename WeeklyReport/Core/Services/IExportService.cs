using OutlookWeeklyReport.Core.Models;

namespace OutlookWeeklyReport.Core.Services;

/// <summary>
/// Interfaccia comune per l'esportazione di report (CSV, XLSX, …).
/// </summary>
public interface IExportService
{
    (string DetailPath, string SummaryPath) Export(
        List<CalendarEvent> events,
        string outputFolder,
        WeekRange week);
}
