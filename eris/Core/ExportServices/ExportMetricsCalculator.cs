using System.Globalization;
using eris.Core.Models;

namespace eris.Core.ExportServices;

public sealed class ExportMetricsCalculator
{
    public ExportMetricsSnapshot Compute(IReadOnlyCollection<CalendarEvent> events, double weeklyHours = 40)
    {
        ArgumentNullException.ThrowIfNull(events);

        var totalMeetingHours = events.Sum(e => e.DurationHours);
        var totalMeetings = events.Count;
        var percentBase = weeklyHours > 0 ? weeklyHours : totalMeetingHours;
        var internalHoursTotal = Math.Max(0, weeklyHours - totalMeetingHours);

        var summaryRows = events
            .GroupBy(e => new
            {
                Category = e.Category ?? string.Empty,
                Client = e.Client ?? string.Empty,
                Project = e.Project ?? string.Empty,
                Topic = e.Category is not null
                    ? string.Empty
                    : e.Project is not null
                        ? string.Empty
                        : e.Topic ?? e.Subject,
                Tag = e.Tag ?? string.Empty,
            })
            .Select(g =>
            {
                var hours = Math.Round(g.Sum(e => e.DurationHours), 2);
                var internalHours = AllocateInternalHours(hours, totalMeetingHours, internalHoursTotal);

                return new ExportSummaryRow(
                    Category: g.Key.Category,
                    Client: g.Key.Client,
                    Project: g.Key.Project,
                    Topic: g.Key.Topic,
                    Tag: g.Key.Tag,
                    MeetingCount: g.Count(),
                    TotalHours: hours,
                    ShareOfWeeklyHours: FormatPercent(hours, percentBase),
                    ShareOfMeetingHours: FormatPercent(hours, totalMeetingHours),
                    InternalHours: internalHours,
                    TotalSpentHours: hours + internalHours);
            })
            .OrderByDescending(r => r.TotalHours)
            .ToList();

        var summaryByTagRows = events
            .GroupBy(e => e.Tag ?? string.Empty)
            .Select(g =>
            {
                var hours = Math.Round(g.Sum(e => e.DurationHours), 2);
                var internalHours = AllocateInternalHours(hours, totalMeetingHours, internalHoursTotal);

                return new ExportTagSummaryRow(
                    Tag: g.Key,
                    MeetingCount: g.Count(),
                    TotalHours: hours,
                    ShareOfWeeklyHours: FormatPercent(hours, percentBase),
                    ShareOfMeetingHours: FormatPercent(hours, totalMeetingHours),
                    InternalHours: internalHours,
                    TotalSpentHours: hours + internalHours);
            })
            .OrderByDescending(r => r.TotalHours)
            .ToList();

        var totals = new ExportSummaryTotals(
            MeetingCount: totalMeetings,
            MeetingHours: Math.Round(totalMeetingHours, 2),
            ShareOfWeeklyHours: FormatPercent(totalMeetingHours, percentBase),
            ShareOfMeetingHours: FormatPercent(totalMeetingHours, totalMeetingHours),
            InternalHours: Math.Round(internalHoursTotal, 2),
            TotalSpentHours: Math.Round(totalMeetingHours + internalHoursTotal, 2));

        return new ExportMetricsSnapshot(
            WeeklyHours: weeklyHours,
            PercentBase: percentBase,
            Totals: totals,
            SummaryRows: summaryRows,
            SummaryByTagRows: summaryByTagRows);
    }

    private static double AllocateInternalHours(double rowHours, double totalMeetingHours, double internalHoursTotal)
    {
        if (rowHours <= 0 || totalMeetingHours <= 0 || internalHoursTotal <= 0)
            return 0;

        return Math.Round(internalHoursTotal * (rowHours / totalMeetingHours), 2);
    }

    private static string FormatPercent(double value, double total)
        => total > 0
            ? string.Create(CultureInfo.InvariantCulture, $"{value / total * 100:F1}%")
            : "0%";
}
