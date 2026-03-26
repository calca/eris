using System.Globalization;

namespace eris.Core.Models;

public enum WeekPeriod { ThisWeek, LastWeek }

/// <summary>
/// Rappresenta un intervallo settimanale ISO 8601 (Lunedì–Domenica).
/// Gestisce correttamente il cambio anno a dicembre/gennaio (es. 30-dic-2024 → Week 1/2025).
/// </summary>
public sealed class WeekRange
{
    public DateTimeOffset Start      { get; }
    public DateTimeOffset End        { get; }   // exclusive (next Monday 00:00)
    public int            WeekNumber { get; }
    public int            Year       { get; }
    /// <summary>Es: "week-12-2026-report"</summary>
    public string FolderName    { get; }
    /// <summary>Es: "Week 12/2026 (17/03/2026 – 23/03/2026)"</summary>
    public string DisplayName   { get; }

    private WeekRange(DateTimeOffset start, DateTimeOffset end)
    {
        Start = start;
        End   = end;

        var cal    = CultureInfo.InvariantCulture.Calendar;
        WeekNumber = cal.GetWeekOfYear(start.DateTime, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        Year       = GetIsoYear(start.DateTime, WeekNumber);

        FolderName  = $"week-{WeekNumber:D2}-{Year}-report";
        DisplayName = $"Week {WeekNumber}/{Year} ({start:dd/MM/yyyy} – {end.AddDays(-1):dd/MM/yyyy})";
    }

    private static int GetIsoYear(DateTime date, int isoWeek)
    {
        // Week ≥ 52 in January  → the week belongs to the previous year
        if (isoWeek >= 52 && date.Month == 1) return date.Year - 1;
        // Week  = 1  in December → the week belongs to the next year
        if (isoWeek == 1  && date.Month == 12) return date.Year + 1;
        return date.Year;
    }

    public static WeekRange FromPeriod(WeekPeriod period, bool workWeek = false)
    {
        var today        = DateTime.Today;
        int daysToMonday = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        var thisMonday   = today.AddDays(-daysToMonday);
        var monday       = period == WeekPeriod.LastWeek ? thisMonday.AddDays(-7) : thisMonday;

        var offset = DateTimeOffset.Now.Offset;
        var start  = new DateTimeOffset(monday, offset);
        // Work week: Mon–Fri (5 days); solar week: Mon–Sun (7 days)
        var daysInRange = workWeek ? 5 : 7;
        return new WeekRange(start, start.AddDays(daysInRange));
    }

    public static WeekRange FromCustom(DateTime start, DateTime end)
    {
        var offset   = DateTimeOffset.Now.Offset;
        var startDto = new DateTimeOffset(start.Date, offset);
        var endDto   = new DateTimeOffset(end.Date.AddDays(1), offset);
        return new WeekRange(startDto, endDto,
            folderName:  $"custom-{start:yyyyMMdd}-{end:yyyyMMdd}-report",
            displayName: $"{start:dd/MM/yyyy} \u2013 {end:dd/MM/yyyy}");
    }

    private WeekRange(DateTimeOffset start, DateTimeOffset end, string folderName, string displayName)
    {
        Start = start;
        End   = end;
        var cal    = CultureInfo.InvariantCulture.Calendar;
        WeekNumber = cal.GetWeekOfYear(start.DateTime, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        Year       = GetIsoYear(start.DateTime, WeekNumber);
        FolderName  = folderName;
        DisplayName = displayName;
    }
}
