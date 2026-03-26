using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using eris.Core.Services;
using eris.UI.ViewModels;
using eris.UI.Views;

namespace eris.UI;

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

        // Per il system browser di MSAL .NET usare sempre una loopback redirect URI.
        // La stessa URI deve essere registrata anche nell'app registration Azure AD.
        pcaBuilder.WithRedirectUri("http://localhost");

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
