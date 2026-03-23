namespace OutlookWeeklyReport.Core.Models;

/// <summary>
/// Rappresenta un evento di calendario estratto da Microsoft Graph.
/// Contiene solo gli eventi accettati o organizzati dall'utente.
/// </summary>
public class CalendarEvent
{
    public string Subject      { get; set; } = string.Empty;
    /// <summary>Prima categoria assegnata all'evento (può essere null).</summary>
    public string? Category    { get; set; }
    /// <summary>Durata in ore (calcolata da start/end UTC restituiti da Graph).</summary>
    public double DurationHours { get; set; }
}
