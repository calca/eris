using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
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

        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
