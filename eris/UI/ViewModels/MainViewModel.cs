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
    [NotifyPropertyChangedFor(nameof(ToggleConfigLabel))]
    private bool _isGenerateTab = true;

    public bool IsConfigTab => !IsGenerateTab;
    public string ToggleConfigLabel => IsConfigTab ? "✕  Chiudi" : "⚙️";

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
    [NotifyPropertyChangedFor(nameof(IsConfigIncomplete))]
    [NotifyCanExecuteChangedFor(nameof(GenerateReportCommand))]
    private bool _isGraphSelected;

    public bool IsGraphSource => IsGraphSelected;
    public bool IsIcsSource   => !IsGraphSelected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsConfigIncomplete))]
    [NotifyCanExecuteChangedFor(nameof(GenerateReportCommand))]
    private string _icsUrl = string.Empty;

    // ── Autenticazione ────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotAuthenticated))]
    [NotifyPropertyChangedFor(nameof(IsConfigIncomplete))]
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
    [NotifyPropertyChangedFor(nameof(IsCurrentMonthSelected))]
    [NotifyPropertyChangedFor(nameof(IsCustomPeriodSelected))]
    [NotifyCanExecuteChangedFor(nameof(GenerateReportCommand))]
    private int _periodSelection = 0; // 0 = Questa settimana  1 = Mese Corrente  2 = Libero

    public bool IsThisWeekSelected     => PeriodSelection == 0;
    public bool IsCurrentMonthSelected => PeriodSelection == 1;
    public bool IsCustomPeriodSelected => PeriodSelection == 2;

    /// <summary>Quando abilitato, il range è limitato a Lunedì–Venerdì (settimana lavorativa).</summary>
    [ObservableProperty]
    private bool _isWorkWeek;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CustomPeriodDisplay))]
    [NotifyCanExecuteChangedFor(nameof(GenerateReportCommand))]
    private DateTime _customStartDate = DateTime.Today;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CustomPeriodDisplay))]
    [NotifyCanExecuteChangedFor(nameof(GenerateReportCommand))]
    private DateTime _customEndDate = DateTime.Today;

    public string CustomPeriodDisplay => $"{CustomStartDate:dd/MM/yyyy} \u2013 {CustomEndDate:dd/MM/yyyy}";

    [ObservableProperty]
    private bool _isDatePickerOpen;

    [ObservableProperty]
    private DateTime _dialogStartDate = DateTime.Today;

    [ObservableProperty]
    private DateTime _dialogEndDate = DateTime.Today;

    public string ThisWeekDisplay => GetPeriodShortDisplay(WeekPeriod.ThisWeek);

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
    [NotifyPropertyChangedFor(nameof(IsConfigIncomplete))]
    [NotifyCanExecuteChangedFor(nameof(GenerateReportCommand))]
    private string _outputFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

    public bool IsConfigIncomplete =>
        string.IsNullOrWhiteSpace(OutputFolder)
        || (IsGraphSelected ? !IsAuthenticated : string.IsNullOrWhiteSpace(IcsUrl));

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
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _errorMessage = string.Empty;
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    // ── Risultato ─────────────────────────────────────────────────────────────

    [ObservableProperty]
    private bool _showResult;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MeetingProgress))]
    private int _meetingCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HoursProgress))]
    private double _totalHours;

    // Ore lavorative settimanali di riferimento: configurabile dall'utente (default 40h = 5 gg * 8h)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HoursProgress))]
    [NotifyPropertyChangedFor(nameof(MeetingProgress))]
    private double _weeklyWorkingHours;

    // Arco pieno = settimana piena (occupazione massima).
    public float HoursProgress   => WeeklyWorkingHours > 0
        ? (float)Math.Clamp(TotalHours / WeeklyWorkingHours, 0.0, 1.0)
        : 0f;
    // Max meeting stimato: 0.5 meeting/h * ore settimanali
    public float MeetingProgress => WeeklyWorkingHours > 0
        ? (float)Math.Clamp(MeetingCount / (0.5 * WeeklyWorkingHours), 0.0, 1.0)
        : 0f;

    [ObservableProperty]
    private string _detailPath = string.Empty;

    [ObservableProperty]
    private string _summaryPath = string.Empty;
    [ObservableProperty]
    private string _resultPeriodStart = string.Empty;

    [ObservableProperty]
    private string _resultPeriodEnd = string.Empty;
    // ─────────────────────────────────────────────────────────────────────────

    public MainViewModel(GraphAuthService authService, AppConfig appConfig)
    {
        _authService = authService;
        _icsUrl = Preferences.Default.Get("IcsUrl", string.Empty);
        _isGraphSelected = Preferences.Default.Get("SourceGraph", false);
        _weeklyWorkingHours = Preferences.Default.Get("WeeklyWorkingHours", appConfig.WeeklyWorkingHours);
        _isWorkWeek = Preferences.Default.Get("IsWorkWeek", false);
        UpdateWeekDisplay();
    }

    partial void OnWeeklyWorkingHoursChanged(double value)
    {
        if (value > 0 && double.IsFinite(value))
            Preferences.Default.Set("WeeklyWorkingHours", value);
    }

    partial void OnIsWorkWeekChanged(bool value)
    {
        Preferences.Default.Set("IsWorkWeek", value);
        UpdateWeekDisplay();
    }

    partial void OnPeriodSelectionChanged(int value) => UpdateWeekDisplay();

    partial void OnCustomStartDateChanged(DateTime value) { if (IsCustomPeriodSelected) UpdateWeekDisplay(); }
    partial void OnCustomEndDateChanged(DateTime value)   { if (IsCustomPeriodSelected) UpdateWeekDisplay(); }

    private void UpdateWeekDisplay()
    {
        var week  = PeriodSelection switch
        {
            1 => WeekRange.FromPeriod(WeekPeriod.LastWeek, IsWorkWeek),
            // Custom period: user sets exact dates, so IsWorkWeek does not restrict them.
            2 => WeekRange.FromCustom(CustomStartDate, CustomEndDate),
            _ => WeekRange.FromPeriod(WeekPeriod.ThisWeek, IsWorkWeek),
        };
        WeekRangeDisplay = $"Dal {week.Start:dd/MM/yyyy} al {week.End.AddDays(-1):dd/MM/yyyy}";
        WeekNumber       = week.WeekNumber;
    }

    // ── Comandi ───────────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        IsBusy = true;

        try
        {
            await _authService.GetAccessTokenAsync(GetPlatformParentWindow());

            UserDisplayName  = await _authService.GetUserDisplayNameAsync();
            IsAuthenticated  = true;
            ErrorMessage     = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Errore autenticazione: {ex.Message}";
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
        ErrorMessage      = string.Empty;
    }

    [RelayCommand]
    private void SelectThisWeek() => PeriodSelection = 0;

    [RelayCommand]
    private void SelectCurrentMonth() => PeriodSelection = 1;

    [RelayCommand]
    private void ToggleWorkWeek() => IsWorkWeek = !IsWorkWeek;

    [RelayCommand]
    private void SelectCustomPeriod()
    {
        DialogStartDate  = IsCustomPeriodSelected ? CustomStartDate : DateTime.Today;
        DialogEndDate    = IsCustomPeriodSelected ? CustomEndDate   : DateTime.Today;
        IsDatePickerOpen = true;
    }

    [RelayCommand]
    private void ConfirmDatePicker()
    {
        CustomStartDate  = DialogStartDate;
        CustomEndDate    = DialogEndDate;
        PeriodSelection  = 2;
        IsDatePickerOpen = false;
    }

    [RelayCommand]
    private void CancelDatePicker() => IsDatePickerOpen = false;

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
        ErrorMessage = string.Empty;

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
            var today = DateTime.Today;
            var range  = PeriodSelection switch
            {
                1 => WeekRange.FromPeriod(WeekPeriod.LastWeek, IsWorkWeek),
                // Custom period: user sets exact dates, so IsWorkWeek does not restrict them.
                2 => WeekRange.FromCustom(CustomStartDate, CustomEndDate),
                _ => WeekRange.FromPeriod(WeekPeriod.ThisWeek, IsWorkWeek),
            };
            var format = IsXlsxSelected ? ExportFormat.Xlsx : ExportFormat.Csv;
            var result = await orchestrator.GenerateAsync(range, OutputFolder, format);

            MeetingCount       = result.EventCount;
            TotalHours         = result.TotalHours;
            WeekNumber         = result.Week.WeekNumber;
            DetailPath         = result.DetailPath;
            SummaryPath        = result.SummaryPath;
            ResultPeriodStart  = result.Week.Start.ToString("dd/MM/yyyy");
            ResultPeriodEnd    = result.Week.End.AddDays(-1).ToString("dd/MM/yyyy");
            ShowResult         = true;
            ErrorMessage       = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Errore: {ex.Message}";
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
    private void NuovoReport()
    {
        ShowResult   = false;
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    private void OpenResult()
    {
        if (string.IsNullOrEmpty(DetailPath)) return;

        try
        {
            if (IsXlsxSelected)
            {
                // Apre direttamente il file xlsx
#if WINDOWS
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(DetailPath) { UseShellExecute = true });
#elif MACCATALYST
                System.Diagnostics.Process.Start("open", $"\"{DetailPath}\"");
#endif
            }
            else
            {
                // CSV: apre la cartella
                var folder = Path.GetDirectoryName(DetailPath);
                if (string.IsNullOrEmpty(folder)) return;
#if WINDOWS
                System.Diagnostics.Process.Start("explorer.exe", $"\"{folder}\"");
#elif MACCATALYST
                System.Diagnostics.Process.Start("open", $"\"{folder}\"");
#endif
            }
        }
        catch { }
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task CopyStatusAsync()
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
            await Clipboard.Default.SetTextAsync(ErrorMessage);
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
