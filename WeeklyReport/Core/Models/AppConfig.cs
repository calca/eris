namespace OutlookWeeklyReport.Core.Models;

/// <summary>
/// Configura ClientId, TenantId e Scopes per Microsoft Graph.
/// Valori di default: ClientId pubblico Microsoft (identico a Azure CLI) — nessuna App Registration necessaria.
/// </summary>
public class AppConfig
{
    public string ClientId { get; set; } = "04b07795-8542-4c4c-9b00-4c4c9b00c4c4";
    public string TenantId { get; set; } = "organizations";
    public string[] Scopes  { get; set; } = ["Calendars.Read", "User.Read"];
}
