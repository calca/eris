namespace eris.Core.Models;

/// <summary>Riga del file summary.csv.</summary>
public class CategorySummary
{
    public string Category   { get; set; } = string.Empty;
    public string Client     { get; set; } = string.Empty;
    public string Project    { get; set; } = string.Empty;
    public string Topic      { get; set; } = string.Empty;
    public string Tag        { get; set; } = string.Empty;
    public int MeetingCount  { get; set; }
    public double TotalHours { get; set; }
    public double InternalHours { get; set; }
    public double TotalSpentHours { get; set; }
    public string Percentage { get; set; } = string.Empty;
}
