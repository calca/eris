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
        try
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Startup error in CreateWindow: {ex}");
            var errorDetails = ex.ToString();

            var copyButton = new Button
            {
                Text = "Copia dettagli",
            };

            copyButton.Clicked += async (_, _) =>
            {
                await Clipboard.Default.SetTextAsync(errorDetails);
            };

            var errorPage = new ContentPage
            {
                Title = "Startup Error",
                Content = new ScrollView
                {
                    Content = new VerticalStackLayout
                    {
                        Padding = new Thickness(16),
                        Spacing = 12,
                        Children =
                        {
                            new Label
                            {
                                Text = "Errore durante l'avvio dell'app.",
                                FontSize = 18,
                                FontAttributes = FontAttributes.Bold,
                            },
                            copyButton,
                            new Label
                            {
                                Text = errorDetails,
                                FontSize = 12,
                            },
                        },
                    },
                },
            };

            return new Window(errorPage);
        }
    }
}
