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

        // ── Filters ───────────────────────────────────────────────────────────
        static string[] ReadList(IConfiguration cfg, string section) =>
            cfg.GetSection(section)
               .GetChildren()
               .Select(c => c.Value)
               .OfType<string>()
               .ToArray();

        var cats     = ReadList(config, "Filters:ExcludedCategories");
        var clients  = ReadList(config, "Filters:ExcludedClients");
        var projects = ReadList(config, "Filters:ExcludedProjects");
        var topics   = ReadList(config, "Filters:ExcludedTopics");

        if (cats.Length     > 0) appConfig.Filters.Categories = cats;
        if (clients.Length  > 0) appConfig.Filters.Clients    = clients;
        if (projects.Length > 0) appConfig.Filters.Projects   = projects;
        if (topics.Length   > 0) appConfig.Filters.Topics     = topics;

        // ── SubjectMappings (optional) ───────────────────────────────────────
        var bySourceKey = config.GetSection("SubjectMappings:BySourceKey");
        foreach (var sourceSection in bySourceKey.GetChildren())
        {
            var sourceKey = sourceSection.Key?.Trim();
            if (string.IsNullOrWhiteSpace(sourceKey))
                continue;

            var entries = sourceSection
                .GetChildren()
                .Select(entry =>
                {
                    var subject = (entry["Subject"] ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(subject))
                        return null;

                    var include = true;
                    if (entry["Include"] is { } includeRaw)
                        include = bool.TryParse(includeRaw, out var parsedInclude) ? parsedInclude : true;

                    var tag = entry["Tag"];
                    if (string.IsNullOrWhiteSpace(tag))
                        tag = null;

                    return new SubjectMappingEntry
                    {
                        Subject = subject,
                        Include = include,
                        Tag = tag,
                    };
                })
                .OfType<SubjectMappingEntry>()
                .ToList();

            if (entries.Count > 0)
                appConfig.SubjectMappings.BySourceKey[sourceKey] = entries;
        }

        return appConfig;
    }
}
