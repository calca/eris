using Ical.Net;
using Ical.Net.DataTypes;
using IcsEvent = Ical.Net.CalendarComponents.CalendarEvent;
using eris.Core.Models;

namespace eris.Core.Services;

/// <summary>
/// Sorgente di eventi basata su un file ICS locale (già scaricato).
/// Espande ricorrenze, filtra per range e esclude eventi non accettati.
/// </summary>
public sealed class IcsCalendarService : ICalendarSource
{
    private readonly string _icsFilePath;

    public IcsCalendarService(string icsFilePath) => _icsFilePath = icsFilePath;

    public Task<List<CalendarEvent>> GetEventsAsync(WeekRange week)
    {
        var text     = File.ReadAllText(_icsFilePath);
        var calendar = Calendar.Load(text);

        var start = week.Start.UtcDateTime;
        var end   = week.End.UtcDateTime;

        var events = new List<CalendarEvent>();

        foreach (var evt in calendar.Events)
        {
            // Escludi eventi rifiutati (subject inizia con "Declined")
            if (evt.Summary != null && evt.Summary.StartsWith("Declined", StringComparison.OrdinalIgnoreCase))
                continue;

            var tentative = IsTentative(evt);

            // Escludi eventi full-day
            if (IsAllDay(evt))
                continue;

            // Espandi ricorrenze nel range
            var occurrences = evt.GetOccurrences(
                new CalDateTime(start),
                new CalDateTime(end));

            foreach (var occurrence in occurrences)
            {
                var evtStart = occurrence.Period.StartTime?.AsDateTimeOffset.LocalDateTime;
                var evtEnd   = occurrence.Period.EndTime?.AsDateTimeOffset.LocalDateTime;

                var duration = occurrence.Period.Duration.TotalHours;
                if (duration <= 0 && occurrence.Period.StartTime != null && occurrence.Period.EndTime != null)
                    duration = (occurrence.Period.EndTime.AsUtc - occurrence.Period.StartTime.AsUtc).TotalHours;

                var calEvent = new CalendarEvent
                {
                    Subject       = evt.Summary ?? string.Empty,
                    Category      = evt.Categories?.FirstOrDefault(),
                    DurationHours = Math.Max(0, duration),
                    StartTime     = evtStart,
                    EndTime       = evtEnd,
                    IsTentative   = tentative,
                };
                CalendarEvent.ParseStructuredSubject(calEvent);
                events.Add(calEvent);
            }
        }

        return Task.FromResult(events);
    }

    private static bool IsTentative(IcsEvent evt)
    {
        // X-MICROSOFT-CDO-BUSYSTATUS = TENTATIVE → evento non accettato
        var busyStatus = evt.Properties
            .FirstOrDefault(p => string.Equals(p.Name, "X-MICROSOFT-CDO-BUSYSTATUS", StringComparison.OrdinalIgnoreCase));

        return busyStatus != null &&
               string.Equals(busyStatus.Value?.ToString(), "TENTATIVE", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAllDay(IcsEvent evt)
    {
        // Ical.Net: IsAllDay è true quando DTSTART è di tipo DATE (senza ora)
        if (evt.IsAllDay) return true;

        // Fallback Microsoft: X-MICROSOFT-CDO-ALLDAYEVENT:TRUE
        var prop = evt.Properties
            .FirstOrDefault(p => string.Equals(p.Name, "X-MICROSOFT-CDO-ALLDAYEVENT", StringComparison.OrdinalIgnoreCase));
        return prop != null &&
               string.Equals(prop.Value?.ToString(), "TRUE", StringComparison.OrdinalIgnoreCase);
    }
}
