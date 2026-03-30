namespace eris.Core.Models;

/// <summary>Tipo di sorgente calendario.</summary>
public enum ReportSourceType { Graph, Ics }

/// <summary>Formato di esportazione del report.</summary>
public enum ExportFormat { Csv, Xlsx }

/// <summary>
/// Configura ClientId, TenantId e Scopes per Microsoft Graph,
/// e la sorgente dati (Graph o ICS).
/// Valori di default: ClientId pubblico Microsoft (identico a Azure CLI) — nessuna App Registration necessaria.
/// </summary>
public class AppConfig
{
    public string ClientId { get; set; } = "14d82eec-204b-4c2f-b7e8-296a70dab67e";
    public string TenantId { get; set; } = "organizations";
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
