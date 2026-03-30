namespace eris.Core.ExportServices;

public sealed record ExportTagSummaryRow(
    string Tag,
    int MeetingCount,
    double TotalHours,
    string ShareOfWeeklyHours,
    string ShareOfMeetingHours,
    double InternalHours,
    double TotalSpentHours);
