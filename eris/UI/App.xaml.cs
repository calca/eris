using eris.UI.Views;

namespace eris.UI;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var mainPage = _services.GetRequiredService<MainPage>();
        var window   = new Window(new NavigationPage(mainPage));

        const int windowWidth  = 600;
        const int windowHeight = 800;

        window.Width         = windowWidth;
        window.Height        = windowHeight;
        window.MinimumWidth  = windowWidth;
        window.MinimumHeight = windowHeight;
        window.MaximumWidth  = windowWidth;
        window.MaximumHeight = windowHeight;

        return window;
    }
}
