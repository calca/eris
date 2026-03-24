namespace OutlookWeeklyReport.Core.Models;

/// <summary>
/// Rappresenta un evento di calendario estratto da Microsoft Graph o da un file ICS.
/// Contiene solo gli eventi accettati o organizzati dall'utente.
/// </summary>
public class CalendarEvent
{
    public string Subject      { get; set; } = string.Empty;
    /// <summary>Prima categoria assegnata all'evento (può essere null).</summary>
    public string? Category    { get; set; }
    /// <summary>Durata in ore (calcolata da start/end UTC restituiti da Graph o DTSTART/DTEND ICS).</summary>
    public double DurationHours { get; set; }

    /// <summary>Cliente estratto dal subject strutturato "CLIENT | PROJECT | TOPIC".</summary>
    public string? Client  { get; set; }
    /// <summary>Progetto estratto dal subject strutturato.</summary>
    public string? Project { get; set; }
    /// <summary>Attività estratta dal subject strutturato.</summary>
    public string? Topic   { get; set; }

    /// <summary>
    /// Se il Subject segue esattamente il formato "CLIENT | PROJECT | TOPIC", popola i tre campi.
    /// Subject con formato diverso (nessun separatore, o meno di 3 segmenti) vengono ignorati.
    /// </summary>
    public static void ParseStructuredSubject(CalendarEvent evt)
    {
        if (string.IsNullOrWhiteSpace(evt.Subject)) return;

        var parts = evt.Subject.Split(" | ");
        if (parts.Length >= 3)
        {
            evt.Client  = parts[0].Trim();
            evt.Project = parts[1].Trim();
            evt.Topic   = string.Join(" | ", parts.Skip(3).Prepend(parts[2])).Trim();
        }
    }
}
