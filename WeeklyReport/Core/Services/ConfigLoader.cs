using Microsoft.Extensions.Configuration;
using OutlookWeeklyReport.Core.Models;

namespace OutlookWeeklyReport.Core.Services;

/// <summary>
/// Carica la configurazione da appsettings.json e/o variabili d'ambiente OWREPORT_*.
/// Funziona senza file di config: in quel caso vengono usati i valori di default (Azure CLI public client).
/// </summary>
public static class ConfigLoader
{
    public static AppConfig Load(string? configPath = null)
    {
        var builder = new ConfigurationBuilder();

        if (configPath != null && File.Exists(configPath))
        {
            builder.AddJsonFile(configPath, optional: false);
        }
        else
        {
            var defaultPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (File.Exists(defaultPath))
                builder.AddJsonFile(defaultPath, optional: true);
        }

        // OWREPORT_AzureAd__ClientId, OWREPORT_AzureAd__TenantId, …  
        builder.AddEnvironmentVariables(prefix: "OWREPORT_");

        var config    = builder.Build();
        var appConfig = new AppConfig();   // valori di default già impostati

        if (config["AzureAd:ClientId"] is { } clientId && !string.IsNullOrWhiteSpace(clientId))
            appConfig.ClientId = clientId;

        if (config["AzureAd:TenantId"] is { } tenantId && !string.IsNullOrWhiteSpace(tenantId))
            appConfig.TenantId = tenantId;

        var scopes = config.GetSection("AzureAd:Scopes")
                           .GetChildren()
                           .Select(c => c.Value)
                           .OfType<string>()
                           .ToArray();
        if (scopes.Length > 0)
            appConfig.Scopes = scopes;

        return appConfig;
    }
}
