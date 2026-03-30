using CommunityToolkit.Mvvm.ComponentModel;

namespace eris.UI.ViewModels;

public sealed partial class MeetingMappingItemViewModel : ObservableObject
{
    [ObservableProperty]
    private bool include = true;

    [ObservableProperty]
    private string subject = string.Empty;

    [ObservableProperty]
    private string tag = string.Empty;
}
