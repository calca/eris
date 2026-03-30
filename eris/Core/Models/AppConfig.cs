namespace eris.Core.Models;

/// <summary>Tipo di sorgente calendario.</summary>
public enum ReportSourceType { Graph, Ics }

/// <summary>Formato di esportazione del report.</summary>
public enum ExportFormat { Csv, Xlsx }

/// <summary>
/// Configura ClientId, TenantId e Scopes per Microsoft Graph,
/// e la sorgente dati (Graph o ICS).
/// Per Graph e richiesta una app registration dedicata: ClientId non e valorizzato di default.
/// </summary>
public sealed class AppConfig
{
    public string ClientId { get; set; } = string.Empty;
    public string TenantId { get; set; } = "common";
    public string[] Scopes  { get; set; } = ["Calendars.Read", "User.Read"];

    /// <summary>Sorgente calendario (default: ICS).</summary>
    public ReportSourceType SourceType { get; set; } = ReportSourceType.Ics;

    /// <summary>URL del file .ics (usato solo quando SourceType = Ics).</summary>
    public string? IcsUrl { get; set; }

    /// <summary>Monte ore lavorative settimanali di riferimento (default: 40h = 5 gg × 8h).</summary>
    public double WeeklyWorkingHours { get; set; } = 40.0;

    /// <summary>Filtri di esclusione applicati agli eventi prima dell'esportazione.</summary>
    public EventFilters Filters { get; set; } = new();

    /// <summary>
    /// Mapping manuali per subject, raggruppati per chiave sorgente hash stabile.
    /// La chiave è calcolata da tipo sorgente + identificatore (URL ICS o account Graph).
    /// </summary>
    public SubjectMappingCollection SubjectMappings { get; set; } = new();
}
