using OutlookWeeklyReport.Core.Models;

namespace OutlookWeeklyReport.Core.Services;

/// <summary>
/// Sorgente generica di eventi di calendario.
/// Implementata da CalendarService (Graph) e IcsCalendarService (file ICS).
/// </summary>
public interface ICalendarSource
{
    Task<List<CalendarEvent>> GetEventsAsync(WeekRange week);
}
