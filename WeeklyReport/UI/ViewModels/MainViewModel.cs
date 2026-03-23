using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Maui.Storage;
using OutlookWeeklyReport.Core.Models;
using OutlookWeeklyReport.Core.Services;

namespace OutlookWeeklyReport.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private GraphAuthService? _authService;

    // ── Autenticazione ────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotAuthenticated))]
    [NotifyCanExecuteChangedFor(nameof(GenerateReportCommand))]
    private bool _isAuthenticated;

    [ObservableProperty]
    private string _userDisplayName = "Non autenticato";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDeviceCode))]
    private string? _deviceCodeMessage;

    public bool HasDeviceCode => DeviceCodeMessage != null;

    public bool IsNotAuthenticated => !IsAuthenticated;

    // ── Settimana ─────────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLastWeekSelected))]
    private bool _isThisWeekSelected = true;

    [ObservableProperty]
    private string _weekRangeDisplay = string.Empty;

    [ObservableProperty]
    private int _weekNumber;

    public bool IsLastWeekSelected => !IsThisWeekSelected;

    // ── Output ────────────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateReportCommand))]
    private string _outputFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

    // ── Stato elaborazione ────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    [NotifyCanExecuteChangedFor(nameof(GenerateReportCommand))]
    [NotifyCanExecuteChangedFor(nameof(SelectFolderCommand))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    private string _statusMessage = string.Empty;
    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    [ObservableProperty]
    private Color _statusColor = Colors.Transparent;

    // ── Risultato ─────────────────────────────────────────────────────────────

    [ObservableProperty]
    private bool _showResult;

    [ObservableProperty]
    private int _meetingCount;

    [ObservableProperty]
    private double _totalHours;

    [ObservableProperty]
    private string _detailCsvPath = string.Empty;

    [ObservableProperty]
    private string _summaryCsvPath = string.Empty;

    // ─────────────────────────────────────────────────────────────────────────

    public MainViewModel() => UpdateWeekDisplay();

    partial void OnIsThisWeekSelectedChanged(bool value) => UpdateWeekDisplay();

    private void UpdateWeekDisplay()
    {
        var period = IsThisWeekSelected ? WeekPeriod.ThisWeek : WeekPeriod.LastWeek;
        var week   = WeekRange.FromPeriod(period);
        WeekRangeDisplay = week.DisplayName;
        WeekNumber       = week.WeekNumber;
    }

    // ── Comandi ───────────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        IsBusy = true;
        SetStatus("Autenticazione in corso…", "#0284c7");

        try
        {
            var config = ConfigLoader.Load();
            _authService = new GraphAuthService(config);

            await _authService.GetAccessTokenAsync(msg => DeviceCodeMessage = msg);

            UserDisplayName  = await _authService.GetUserDisplayNameAsync();
            IsAuthenticated  = true;
            DeviceCodeMessage = null;
            SetStatus($"Connesso come {UserDisplayName}", "#16a34a");
        }
        catch (Exception ex)
        {
            SetStatus($"Errore autenticazione: {ex.Message}", "#dc2626");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanLogin() => !IsBusy;

    [RelayCommand]
    private async Task LogoutAsync()
    {
        if (_authService != null)
            await _authService.SignOutAsync();

        IsAuthenticated   = false;
        UserDisplayName   = "Non autenticato";
        DeviceCodeMessage = null;
        ShowResult        = false;
        SetStatus("Disconnesso", "#64748b");
    }

    [RelayCommand]
    private void SelectThisWeek() => IsThisWeekSelected = true;

    [RelayCommand]
    private void SelectLastWeek() => IsThisWeekSelected = false;

    [RelayCommand(CanExecute = nameof(CanSelectFolder))]
    private async Task SelectFolderAsync()
    {
        try
        {
            var result = await FolderPicker.Default.PickAsync(CancellationToken.None);
            if (result.IsSuccessful)
                OutputFolder = result.Folder.Path;
        }
        catch
        {
            // FolderPicker non disponibile sulla piattaforma: l'utente può digitare il path manualmente
        }
    }

    private bool CanSelectFolder() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanGenerate))]
    private async Task GenerateReportAsync()
    {
        if (_authService == null) return;

        IsBusy     = true;
        ShowResult = false;
        SetStatus("Generazione report in corso…", "#0284c7");

        try
        {
            var orchestrator = new ReportOrchestrator(_authService);
            var period       = IsThisWeekSelected ? WeekPeriod.ThisWeek : WeekPeriod.LastWeek;
            var result       = await orchestrator.GenerateAsync(period, OutputFolder);

            MeetingCount   = result.EventCount;
            TotalHours     = result.TotalHours;
            WeekNumber     = result.Week.WeekNumber;
            DetailCsvPath  = result.DetailCsvPath;
            SummaryCsvPath = result.SummaryCsvPath;
            ShowResult     = true;
            SetStatus("Report generato con successo!", "#16a34a");
        }
        catch (Exception ex)
        {
            SetStatus($"Errore: {ex.Message}", "#dc2626");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanGenerate() => IsAuthenticated && !IsBusy && !string.IsNullOrWhiteSpace(OutputFolder);

    [RelayCommand]
    private void OpenResultFolder()
    {
        if (string.IsNullOrEmpty(DetailCsvPath)) return;
        var folder = Path.GetDirectoryName(DetailCsvPath);
        if (string.IsNullOrEmpty(folder)) return;

        try
        {
#if WINDOWS
            System.Diagnostics.Process.Start("explorer.exe", $"\"{folder}\"");
#elif MACCATALYST
            System.Diagnostics.Process.Start("open", $"\"{folder}\"");
#endif
        }
        catch { /* Ignora se l'apertura fallisce */ }
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private void SetStatus(string message, string hexColor)
    {
        StatusMessage = message;
        StatusColor   = Color.FromArgb(hexColor);
    }
}
