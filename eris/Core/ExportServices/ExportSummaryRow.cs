namespace eris.Core.ExportServices;

public sealed record ExportSummaryRow(
    string Category,
    string Client,
    string Project,
    string Topic,
    string Tag,
    int MeetingCount,
    double TotalHours,
    string ShareOfWeeklyHours,
    string ShareOfMeetingHours,
    double InternalHours,
    double TotalSpentHours);
