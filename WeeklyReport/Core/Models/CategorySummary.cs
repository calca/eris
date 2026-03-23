namespace OutlookWeeklyReport.Core.Models;

/// <summary>Riga del file summary.csv.</summary>
public class CategorySummary
{
    public string Label       { get; set; } = string.Empty;
    public double TotalHours  { get; set; }
    public string Percentage  { get; set; } = string.Empty;
}
