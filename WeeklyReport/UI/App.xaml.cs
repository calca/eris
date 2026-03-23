using OutlookWeeklyReport.UI.Views;

namespace OutlookWeeklyReport.UI;

public partial class App : Application
{
    public App(MainPage mainPage)
    {
        InitializeComponent();
        MainPage = new NavigationPage(mainPage);
    }
}
