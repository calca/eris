using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
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
        builder.Services.AddSingleton<IGraphAuthClientFactory, GraphAuthClientFactory>();
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
