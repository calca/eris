using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using eris.Core.Services;
using eris.UI.ViewModels;
using eris.UI.Views;

namespace eris.UI;

public static class MauiProgram
{
    private static PublicClientApplicationBuilder ConfigureAuthority(
        PublicClientApplicationBuilder builder,
        string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId) ||
            tenantId.Equals("organizations", StringComparison.OrdinalIgnoreCase))
            return builder.WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs);

        if (tenantId.Equals("common", StringComparison.OrdinalIgnoreCase))
            return builder.WithAuthority(AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount);

        if (tenantId.Equals("consumers", StringComparison.OrdinalIgnoreCase))
            return builder.WithAuthority(AadAuthorityAudience.PersonalMicrosoftAccount);

        return builder.WithAuthority(AzureCloudInstance.AzurePublic, tenantId);
    }

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit();

        // ── MSAL ──────────────────────────────────────────────────────────────
        var config = ConfigLoader.Load();

        var pcaBuilder = ConfigureAuthority(
            PublicClientApplicationBuilder.Create(config.ClientId),
            config.TenantId);

        // Su Apple (iOS/Mac Catalyst) il redirect deve usare lo schema custom msal{ClientId}://auth
        // registrato nel Info.plist. Su desktop/CLI resta valido il loopback localhost.
    #if IOS || MACCATALYST
        pcaBuilder.WithRedirectUri($"msal{config.ClientId}://auth");
    #else
        pcaBuilder.WithRedirectUri("http://localhost");
    #endif

        var pca = pcaBuilder.Build();

        builder.Services.AddSingleton<IPublicClientApplication>(pca);
        builder.Services.AddSingleton(new GraphAuthService(pca, config.Scopes));
        builder.Services.AddSingleton(config);

        // ── Pages / ViewModels ────────────────────────────────────────────────
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
