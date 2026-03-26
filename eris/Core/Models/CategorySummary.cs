namespace eris.Core.Models;

/// <summary>Riga del file summary.csv.</summary>
public class CategorySummary
{
    public string Category   { get; set; } = string.Empty;
    public string Client     { get; set; } = string.Empty;
    public string Project    { get; set; } = string.Empty;
    public string Topic      { get; set; } = string.Empty;
    public double TotalHours { get; set; }
    public string Percentage { get; set; } = string.Empty;
}
