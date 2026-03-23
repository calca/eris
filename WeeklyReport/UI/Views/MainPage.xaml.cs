using OutlookWeeklyReport.UI.ViewModels;

namespace OutlookWeeklyReport.UI.Views;

public partial class MainPage : ContentPage
{
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
