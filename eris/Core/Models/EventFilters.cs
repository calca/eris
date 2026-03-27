namespace eris.Core.Models;

/// <summary>
/// Filtri di esclusione applicati agli eventi del calendario prima dell'esportazione.
/// Ogni lista è confrontata in modo case-insensitive contro il campo corrispondente dell'evento.
/// Un evento viene escluso se il suo campo corrisponde a un valore presente nella lista relativa
/// (logica OR tra le liste: basta che un qualsiasi campo coincida per escludere l'evento).
/// </summary>
public class EventFilters
{
    /// <summary>Categorie (tag) da escludere (es. "Personale", "OOO").</summary>
    public string[] Categories { get; set; } = [];

    /// <summary>Clienti da escludere (confrontato con CalendarEvent.Client).</summary>
    public string[] Clients    { get; set; } = [];

    /// <summary>Progetti da escludere (confrontato con CalendarEvent.Project).</summary>
    public string[] Projects   { get; set; } = [];

    /// <summary>Topic da escludere (confrontato con CalendarEvent.Topic).</summary>
    public string[] Topics     { get; set; } = [];

    /// <summary>True se tutti e quattro i filtri sono vuoti (nessuna esclusione configurata).</summary>
    public bool IsEmpty =>
        Categories.Length == 0 &&
        Clients.Length    == 0 &&
        Projects.Length   == 0 &&
        Topics.Length     == 0;
}
