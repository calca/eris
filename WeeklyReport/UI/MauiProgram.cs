using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using OutlookWeeklyReport.Core.Services;
using OutlookWeeklyReport.UI.ViewModels;
using OutlookWeeklyReport.UI.Views;

namespace OutlookWeeklyReport.UI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit();

        // ── MSAL ──────────────────────────────────────────────────────────────
        var config = ConfigLoader.Load();

        var pcaBuilder = PublicClientApplicationBuilder
            .Create(config.ClientId)
            .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs);

#if MACCATALYST
        // Mac Catalyst: usa lo schema msal{clientId}://auth che attiva
        // ASWebAuthenticationSession (login nativo Apple SSO).
        pcaBuilder.WithRedirectUri($"msal{config.ClientId}://auth");
#else
        pcaBuilder.WithRedirectUri("http://localhost");
#endif

        var pca = pcaBuilder.Build();

        builder.Services.AddSingleton<IPublicClientApplication>(pca);
        builder.Services.AddSingleton(new GraphAuthService(pca, config.Scopes));

        // ── Pages / ViewModels ────────────────────────────────────────────────
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
