using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Maui.Storage;
using eris.Core.Models;
using eris.Core.Services;

namespace eris.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly GraphAuthService _authService;

    // ── Tab ───────────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsConfigTab))]
    private bool _isGenerateTab = true;

    public bool IsConfigTab => !IsGenerateTab;

    [RelayCommand]
    private void SelectTabGenerate() => IsGenerateTab = true;

    [RelayCommand]
    private void SelectTabConfig() => IsGenerateTab = false;

    [RelayCommand]
    private void ToggleConfig() => IsGenerateTab = !IsGenerateTab;

    // ── Sorgente ──────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIcsSource))]
    [NotifyPropertyChangedFor(nameof(IsGraphSource))]
    [NotifyCanExecuteChangedFor(nameof(GenerateReportCommand))]
    private bool _isGraphSelected;

    public bool IsGraphSource => IsGraphSelected;
    public bool IsIcsSource   => !IsGraphSelected;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateReportCommand))]
    private string _icsUrl = string.Empty;

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

    // ── Periodo ───────────────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsThisWeekSelected))]
    [NotifyPropertyChangedFor(nameof(IsLastWeekSelected))]
    [NotifyPropertyChangedFor(nameof(IsCustomPeriodSelected))]
    [NotifyCanExecuteChangedFor(nameof(GenerateReportCommand))]
    private int _periodSelection = 0; // 0 = Questa settimana  1 = Scorsa  2 = Libero

    public bool IsThisWeekSelected     => _periodSelection == 0;
    public bool IsLastWeekSelected     => _periodSelection == 1;
    public bool IsCustomPeriodSelected => _periodSelection == 2;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CustomPeriodDisplay))]
    [NotifyCanExecuteChangedFor(nameof(GenerateReportCommand))]
    private DateTime _customStartDate = DateTime.Today;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CustomPeriodDisplay))]
    [NotifyCanExecuteChangedFor(nameof(GenerateReportCommand))]
    private DateTime _customEndDate = DateTime.Today;

    public string CustomPeriodDisplay => $"{CustomStartDate:dd/MM/yyyy} \u2013 {CustomEndDate:dd/MM/yyyy}";

    public string ThisWeekDisplay => GetPeriodShortDisplay(WeekPeriod.ThisWeek);
    public string LastWeekDisplay => GetPeriodShortDisplay(WeekPeriod.LastWeek);

    [ObservableProperty]
    private string _weekRangeDisplay = string.Empty;

    [ObservableProperty]
    private int _weekNumber;

    private static string GetPeriodShortDisplay(WeekPeriod period)
    {
        var week = WeekRange.FromPeriod(period);
        return $"{week.Start:dd/MM} \u2013 {week.End.AddDays(-1):dd/MM}";
    }

    // ── Output ────────────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateReportCommand))]
    private string _outputFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

    // ── Formato ───────────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCsvFormat))]
    [NotifyPropertyChangedFor(nameof(IsXlsxFormat))]
    private bool _isXlsxSelected = true;

    public bool IsXlsxFormat => IsXlsxSelected;
    public bool IsCsvFormat  => !IsXlsxSelected;

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
    private string _detailPath = string.Empty;

    [ObservableProperty]
    private string _summaryPath = string.Empty;

    // ─────────────────────────────────────────────────────────────────────────

    public MainViewModel(GraphAuthService authService)
    {
        _authService = authService;
        _icsUrl = Preferences.Default.Get("IcsUrl", string.Empty);
        _isGraphSelected = Preferences.Default.Get("SourceGraph", false);
        UpdateWeekDisplay();
    }

    partial void OnPeriodSelectionChanged(int value) => UpdateWeekDisplay();

    partial void OnCustomStartDateChanged(DateTime value) { if (IsCustomPeriodSelected) UpdateWeekDisplay(); }
    partial void OnCustomEndDateChanged(DateTime value)   { if (IsCustomPeriodSelected) UpdateWeekDisplay(); }

    private void UpdateWeekDisplay()
    {
        var week = _periodSelection == 2
            ? WeekRange.FromCustom(CustomStartDate, CustomEndDate)
            : WeekRange.FromPeriod(_periodSelection == 0 ? WeekPeriod.ThisWeek : WeekPeriod.LastWeek);
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
            await _authService.GetAccessTokenAsync(GetPlatformParentWindow());

            UserDisplayName  = await _authService.GetUserDisplayNameAsync();
            IsAuthenticated  = true;
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
    private void SelectThisWeek() => PeriodSelection = 0;

    [RelayCommand]
    private void SelectLastWeek() => PeriodSelection = 1;

    [RelayCommand]
    private void SelectCustomPeriod() => PeriodSelection = 2;

    [RelayCommand]
    private void SelectSourceGraph()
    {
        IsGraphSelected = true;
        Preferences.Default.Set("SourceGraph", true);
    }

    [RelayCommand]
    private void SelectSourceIcs()
    {
        IsGraphSelected = false;
        Preferences.Default.Set("SourceGraph", false);
    }

    [RelayCommand]
    private void SelectFormatXlsx() => IsXlsxSelected = true;

    [RelayCommand]
    private void SelectFormatCsv() => IsXlsxSelected = false;

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
        IsBusy     = true;
        ShowResult = false;
        SetStatus("Generazione report in corso…", "#0284c7");

        try
        {
            ICalendarSource calendarSource;

            if (IsGraphSelected)
            {
                var token = await _authService.GetAccessTokenAsync(GetPlatformParentWindow());
                calendarSource = new CalendarService(token);
            }
            else
            {
                // Persist URL for next launch
                Preferences.Default.Set("IcsUrl", IcsUrl);
                Preferences.Default.Set("SourceGraph", false);

                var downloader = new IcsDownloadService();
                var localPath  = await downloader.DownloadAsync(IcsUrl);
                calendarSource = new IcsCalendarService(localPath);
            }

            var orchestrator = new ReportOrchestrator(calendarSource);
            var range  = _periodSelection == 2
                ? WeekRange.FromCustom(CustomStartDate, CustomEndDate)
                : WeekRange.FromPeriod(_periodSelection == 0 ? WeekPeriod.ThisWeek : WeekPeriod.LastWeek);
            var format = IsXlsxSelected ? ExportFormat.Xlsx : ExportFormat.Csv;
            var result = await orchestrator.GenerateAsync(range, OutputFolder, format);

            MeetingCount = result.EventCount;
            TotalHours   = result.TotalHours;
            WeekNumber   = result.Week.WeekNumber;
            DetailPath   = result.DetailPath;
            SummaryPath  = result.SummaryPath;
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

    private bool CanGenerate() => !IsBusy
        && !string.IsNullOrWhiteSpace(OutputFolder)
        && (IsGraphSelected ? IsAuthenticated : !string.IsNullOrWhiteSpace(IcsUrl))
        && (!IsCustomPeriodSelected || CustomEndDate >= CustomStartDate);

    [RelayCommand]
    private void OpenResultFolder()
    {
        if (string.IsNullOrEmpty(DetailPath)) return;
        var folder = Path.GetDirectoryName(DetailPath);
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

    [RelayCommand]
    private async Task CopyStatusAsync()
    {
        if (!string.IsNullOrEmpty(StatusMessage))
            await Clipboard.Default.SetTextAsync(StatusMessage);
    }

    private void SetStatus(string message, string hexColor)
    {
        StatusMessage = message;
        StatusColor   = Color.FromArgb(hexColor);
    }

    /// <summary>
    /// Restituisce la parent window/VC per presentare la UI di autenticazione MSAL.
    /// </summary>
    private static object? GetPlatformParentWindow()
    {
#if MACCATALYST || IOS
        // Ottiene il UIViewController root dalla finestra corrente MAUI
        var window = Application.Current?.Windows.FirstOrDefault();
        var platformWindow = window?.Handler?.PlatformView as UIKit.UIWindow;
        return platformWindow?.RootViewController;
#elif WINDOWS
        var window = Application.Current?.Windows.FirstOrDefault();
        if (window?.Handler?.PlatformView is Microsoft.UI.Xaml.Window winUiWindow)
        {
            var handle = WinRT.Interop.WindowNative.GetWindowHandle(winUiWindow);
            return handle;
        }
        return null;
#else
        return null;
#endif
    }
}
