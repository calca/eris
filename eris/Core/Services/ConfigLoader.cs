using Microsoft.Extensions.Configuration;
using eris.Core.Models;

namespace eris.Core.Services;

/// <summary>
/// Carica la configurazione da appsettings.json e/o variabili d'ambiente ERIS_*.
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

        // ERIS_AzureAd__ClientId, ERIS_AzureAd__TenantId, …  
        builder.AddEnvironmentVariables(prefix: "ERIS_");

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

        // ── Source ────────────────────────────────────────────────────────────
        if (config["Source:Type"] is { } sourceType && !string.IsNullOrWhiteSpace(sourceType))
        {
            if (Enum.TryParse<ReportSourceType>(sourceType, ignoreCase: true, out var parsed))
                appConfig.SourceType = parsed;
        }

        if (config["Source:IcsUrl"] is { } icsUrl && !string.IsNullOrWhiteSpace(icsUrl))
            appConfig.IcsUrl = icsUrl;

        // ── WorkingHours ──────────────────────────────────────────────────────
        if (config["WorkingHours:WeeklyTarget"] is { } weeklyTarget
            && double.TryParse(weeklyTarget, System.Globalization.NumberStyles.Any,
                               System.Globalization.CultureInfo.InvariantCulture, out var hours)
            && hours > 0)
        {
            appConfig.WeeklyWorkingHours = hours;
        }

        return appConfig;
    }
}
