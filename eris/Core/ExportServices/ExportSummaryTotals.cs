namespace eris.Core.ExportServices;

public sealed record ExportSummaryTotals(
    int MeetingCount,
    double MeetingHours,
    string ShareOfWeeklyHours,
    string ShareOfMeetingHours,
    double InternalHours,
    double TotalSpentHours);
