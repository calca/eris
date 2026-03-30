using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Maui.Storage;
using Microsoft.Maui.ApplicationModel;
using eris.Core.Models;
using eris.Core.Services;
using eris.UI.Resources.Strings;

namespace eris.UI.ViewModels;

/// <summary>Wrapper to allow proper x:DataType in XAML DataTemplate (avoids compiled binding inheritance issues).</summary>
public sealed class TemplateItem(string value)
{
    public string Value { get; } = value;
}

public partial class MainViewModel : ObservableObject
{
    private const string SubjectMappingsPreferenceKey = "SubjectMappingsBySource";

    private readonly GraphAuthService _authService;
    private SubjectMappingCollection _subjectMappings = new();
    private ReportOrchestrator? _pendingOrchestrator;
    private List<CalendarEvent>? _pendingExtractedEvents;
    private WeekRange? _pendingRange;
    private ExportFormat _pendingFormat;
    private EventFilters? _pendingFilters;
    private string? _pendingSourceKey;
    private string? _editOnlySourceKey;

    // ── Tab ───────────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsConfigTab))]
    [NotifyPropertyChangedFor(nameof(ToggleConfigLabel))]
    [NotifyPropertyChangedFor(nameof(PageTitle))]
    private bool _isGenerateTab = true;

    public bool IsConfigTab => !IsGenerateTab;
    public string PageTitle => IsConfigTab ? AppStrings.Settings : AppStrings.YourReports;
    public string ToggleConfigLabel => IsConfigTab ? "🏠" : "⚙️";

    [RelayCommand]
    private void SelectTabGenerate() => IsGenerateTab = true;

    [RelayCommand]
    private void SelectTabConfig() => IsGenerateTab = false;

    [RelayCommand]
    private void ToggleConfig() => IsGenerateTab = !IsGenerateTab;

    // ── Tema ──────────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ThemeLabel))]
    private bool _isDarkTheme = Application.Current!.RequestedTheme == AppTheme.Dark;

    public string ThemeLabel => IsDarkTheme ? "☀️" : "🌙";

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        Application.Current!.UserAppTheme = IsDarkTheme ? AppTheme.Dark : AppTheme.Light;
    }

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

    [RelayCommand]
    private async Task ShowIcsHelp()
    {
        await Application.Current!.Windows[0].Page!.DisplayAlertAsync(
            AppStrings.IcsHelpTitle,
            AppStrings.IcsHelpBody,
            AppStrings.Ok);
    }

    // ── Autenticazione ────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotAuthenticated))]
    [NotifyPropertyChangedFor(nameof(IsConfigIncomplete))]
    [NotifyCanExecuteChangedFor(nameof(GenerateReportCommand))]
    private bool _isAuthenticated;

    [ObservableProperty]
    private string _userDisplayName = AppStrings.NotAuthenticated;

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
    [NotifyPropertyChangedFor(nameof(WorkWeekLabel))]
    private bool _isWorkWeek;

    public string WorkWeekLabel => IsWorkWeek ? AppStrings.MonFri : AppStrings.MonSun;

    /// <summary>Categorie (tag) degli eventi da escludere, separate da virgola (es. "Personale, OOO").</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActiveFilters))]
    private string _excludedCategories = string.Empty;

    /// <summary>Clienti da escludere dal report, separati da virgola.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActiveFilters))]
    private string _excludedClients = string.Empty;

    /// <summary>Progetti da escludere dal report, separati da virgola.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActiveFilters))]
    private string _excludedProjects = string.Empty;

    /// <summary>Topic da escludere dal report, separati da virgola.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActiveFilters))]
    private string _excludedTopics = string.Empty;

    /// <summary>Se true, gli eventi tentative vengono esclusi dal report.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActiveFilters))]
    private bool _excludeTentative = true;

    /// <summary>Template per il parsing del subject strutturato (multipli, provati in ordine).</summary>
    [ObservableProperty]
    private ObservableCollection<string> _subjectTemplates = new();

    public bool HasActiveFilters => ExcludeTentative
        || HasFilterValues(ExcludedCategories)
        || HasFilterValues(ExcludedClients)
        || HasFilterValues(ExcludedProjects)
        || HasFilterValues(ExcludedTopics);

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
    private bool _isFiltersDialogOpen;

    [ObservableProperty]
    private bool _isTemplateDialogOpen;

    [ObservableProperty]
    private ObservableCollection<TemplateItem> _dialogSubjectTemplates = new();

    [ObservableProperty]
    private string _newDialogTemplate = string.Empty;

    [ObservableProperty]
    private string _dialogExcludedCategories = string.Empty;

    [ObservableProperty]
    private string _dialogExcludedClients = string.Empty;

    [ObservableProperty]
    private string _dialogExcludedProjects = string.Empty;

    [ObservableProperty]
    private string _dialogExcludedTopics = string.Empty;

    [ObservableProperty]
    private bool _dialogExcludeTentative = true;

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
    [NotifyPropertyChangedFor(nameof(OutputFolderName))]
    [NotifyCanExecuteChangedFor(nameof(GenerateReportCommand))]
    private string _outputFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

    public string OutputFolderName =>
        string.IsNullOrWhiteSpace(OutputFolder)
            ? AppStrings.NoFolderSelected
            : Path.GetFileName(OutputFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

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
    [NotifyCanExecuteChangedFor(nameof(GenerateMappedReportCommand))]
    [NotifyCanExecuteChangedFor(nameof(SelectFolderCommand))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateMappedReportCommand))]
    private bool _isMappingPageOpen;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MappingPrimaryButtonLabel))]
    [NotifyCanExecuteChangedFor(nameof(GenerateMappedReportCommand))]
    private bool _isMappingEditOnly;

    public string MappingPrimaryButtonLabel => IsMappingEditOnly ? AppStrings.Save : AppStrings.GenerateReport;

    [ObservableProperty]
    private ObservableCollection<MeetingMappingItemViewModel> _mappingItems = new();

    [ObservableProperty]
    private bool _isMappingEnabled;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _errorMessage = string.Empty;
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAuthError))]
    private string _authErrorMessage = string.Empty;
    public bool HasAuthError => !string.IsNullOrEmpty(AuthErrorMessage);

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
        _subjectMappings = LoadSubjectMappings();
        _icsUrl = Preferences.Default.Get("IcsUrl", string.Empty);
        _isGraphSelected = Preferences.Default.Get("SourceGraph", false);
        _weeklyWorkingHours = Preferences.Default.Get("WeeklyWorkingHours", appConfig.WeeklyWorkingHours);
        _isWorkWeek = Preferences.Default.Get("IsWorkWeek", false);
        _excludedCategories = Preferences.Default.Get("ExcludedCategories", string.Join(", ", appConfig.Filters.Categories));
        _excludedClients    = Preferences.Default.Get("ExcludedClients",    string.Join(", ", appConfig.Filters.Clients));
        _excludedProjects   = Preferences.Default.Get("ExcludedProjects",   string.Join(", ", appConfig.Filters.Projects));
        _excludedTopics     = Preferences.Default.Get("ExcludedTopics",     string.Join(", ", appConfig.Filters.Topics));
        _excludeTentative   = Preferences.Default.Get("ExcludeTentative", true);
        _isMappingEnabled   = Preferences.Default.Get("IsMappingEnabled", false);
        _subjectTemplates   = LoadSubjectTemplates();
        UpdateWeekDisplay();
    }

    partial void OnWeeklyWorkingHoursChanged(double value)
    {
        if (value > 0 && double.IsFinite(value))
            Preferences.Default.Set("WeeklyWorkingHours", value);
    }

    partial void OnExcludedCategoriesChanged(string value) => Preferences.Default.Set("ExcludedCategories", value);
    partial void OnExcludedClientsChanged(string value)    => Preferences.Default.Set("ExcludedClients",    value);
    partial void OnExcludedProjectsChanged(string value)   => Preferences.Default.Set("ExcludedProjects",   value);
    partial void OnExcludedTopicsChanged(string value)     => Preferences.Default.Set("ExcludedTopics",     value);
    partial void OnExcludeTentativeChanged(bool value)       => Preferences.Default.Set("ExcludeTentative",   value);
    partial void OnIsMappingEnabledChanged(bool value)       => Preferences.Default.Set("IsMappingEnabled", value);

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
        WeekRangeDisplay = string.Format(AppStrings.DateRangeFormat, week.Start.ToString("dd/MM/yyyy"), week.End.AddDays(-1).ToString("dd/MM/yyyy"));
        WeekNumber       = week.WeekNumber;
    }

    // ── Comandi ───────────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        IsBusy = true;
        DeviceCodeMessage = null;

        try
        {
            await _authService.GetAccessTokenWithMacCatalystFallbackAsync(
                GetPlatformParentWindow(),
                message =>
                {
                    MainThread.BeginInvokeOnMainThread(() => DeviceCodeMessage = message);
                    return Task.CompletedTask;
                });

            UserDisplayName  = await _authService.GetUserDisplayNameAsync();
            IsAuthenticated  = true;
            DeviceCodeMessage = null;
            AuthErrorMessage = string.Empty;
            ErrorMessage     = string.Empty;
        }
        catch (Exception ex)
        {
            AuthErrorMessage = string.Format(AppStrings.AuthErrorFormat, ex.Message);
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
        UserDisplayName   = AppStrings.NotAuthenticated;
        DeviceCodeMessage = null;
        AuthErrorMessage  = string.Empty;
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
    private void SelectCustomPeriod() => PeriodSelection = 2;

    [RelayCommand]
    private void OpenDatePicker()
    {
        if (!IsCustomPeriodSelected) return;
        DialogStartDate  = CustomStartDate;
        DialogEndDate    = CustomEndDate;
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
    private void OpenTemplateDialog()
    {
        DialogSubjectTemplates = new ObservableCollection<TemplateItem>(SubjectTemplates.Select(t => new TemplateItem(t)));
        NewDialogTemplate = string.Empty;
        IsTemplateDialogOpen = true;
    }

    [RelayCommand]
    private void CancelTemplateDialog() => IsTemplateDialogOpen = false;

    [RelayCommand]
    private void ApplyTemplateDialog()
    {
        SubjectTemplates = new ObservableCollection<string>(DialogSubjectTemplates.Select(t => t.Value));
        SaveSubjectTemplates();
        IsTemplateDialogOpen = false;
    }

    [RelayCommand]
    private void ResetTemplateDialog()
    {
        DialogSubjectTemplates = new ObservableCollection<TemplateItem>([new TemplateItem(CalendarEvent.DefaultTemplate)]);
    }

    [RelayCommand]
    private void AddDialogTemplate()
    {
        if (string.IsNullOrWhiteSpace(NewDialogTemplate)) return;
        DialogSubjectTemplates.Add(new TemplateItem(NewDialogTemplate.Trim()));
        NewDialogTemplate = string.Empty;
    }

    [RelayCommand]
    private void RemoveDialogTemplate(TemplateItem item)
    {
        DialogSubjectTemplates.Remove(item);
    }

    [RelayCommand]
    private void OpenFiltersDialog()
    {
        DialogExcludedCategories = ExcludedCategories;
        DialogExcludedClients = ExcludedClients;
        DialogExcludedProjects = ExcludedProjects;
        DialogExcludedTopics = ExcludedTopics;
        DialogExcludeTentative = ExcludeTentative;
        IsFiltersDialogOpen = true;
    }

    [RelayCommand]
    private void CancelFiltersDialog() => IsFiltersDialogOpen = false;

    [RelayCommand]
    private void ApplyFiltersDialog()
    {
        ExcludedCategories = DialogExcludedCategories;
        ExcludedClients = DialogExcludedClients;
        ExcludedProjects = DialogExcludedProjects;
        ExcludedTopics = DialogExcludedTopics;
        ExcludeTentative = DialogExcludeTentative;
        IsFiltersDialogOpen = false;
    }

    [RelayCommand]
    private void SelectSourceGraph()
    {
        IsGraphSelected = true;
        AuthErrorMessage = string.Empty;
        Preferences.Default.Set("SourceGraph", true);
    }

    [RelayCommand]
    private void SelectSourceIcs()
    {
        IsGraphSelected = false;
        AuthErrorMessage = string.Empty;
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

    [RelayCommand]
    private async Task OpenOutputFolderAsync()
    {
        if (!string.IsNullOrWhiteSpace(OutputFolder) && Directory.Exists(OutputFolder))
            await Launcher.OpenAsync(new Uri($"file://{OutputFolder}"));
    }

    [RelayCommand(CanExecute = nameof(CanGenerate))]
    private async Task GenerateReportAsync()
    {
        IsBusy     = true;
        ShowResult = false;
        IsMappingPageOpen = false;
        ErrorMessage = string.Empty;

        try
        {
            var (orchestrator, events, range, format, filters, sourceKey) = await ExtractGenerationContextAsync();

            foreach (var evt in events)
                CalendarEvent.ParseStructuredSubject(evt, SubjectTemplates.ToList());

            var filteredForMapping = ApplyExclusionsForMapping(events, filters);

            if (IsMappingEnabled)
            {
                var missingSubjects = GetSubjectsWithoutTag(filteredForMapping, sourceKey);
                if (missingSubjects.Count > 0)
                {
                    var allFilteredSubjects = filteredForMapping
                        .Select(x => (x.Subject ?? string.Empty).Trim())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    _pendingOrchestrator = orchestrator;
                    _pendingExtractedEvents = events;
                    _pendingRange = range;
                    _pendingFormat = format;
                    _pendingFilters = filters;
                    _pendingSourceKey = sourceKey;
                    _editOnlySourceKey = null;
                    IsMappingEditOnly = false;

                    MappingItems = BuildMappingItems(allFilteredSubjects, sourceKey);
                    IsMappingPageOpen = true;
                    return;
                }
            }

            var result = await orchestrator.GenerateAsync(
                range,
                OutputFolder,
                format,
                filters,
                SubjectTemplates.ToList(),
                WeeklyWorkingHours,
                IsMappingEnabled ? _subjectMappings : null,
                IsMappingEnabled ? sourceKey : null);

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
            ErrorMessage = string.Format(AppStrings.GenericErrorFormat, ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void BackFromMapping()
    {
        IsMappingPageOpen = false;
        IsMappingEditOnly = false;
        _editOnlySourceKey = null;
    }

    [RelayCommand]
    private async Task OpenTagEditorAsync()
    {
        ErrorMessage = string.Empty;

        var sourceKey = await ResolveCurrentSourceKeyAsync();
        if (string.IsNullOrWhiteSpace(sourceKey))
            return;

        var existingSubjects = _subjectMappings
            .GetForSourceKey(sourceKey)
            .Select(x => x.Subject)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (existingSubjects.Count == 0)
        {
            ErrorMessage = AppStrings.NoMappedTagsToEdit;
            return;
        }

        MappingItems = BuildMappingItems(existingSubjects, sourceKey);
        _editOnlySourceKey = sourceKey;
        IsMappingEditOnly = true;
        IsMappingPageOpen = true;
        IsGenerateTab = true;
    }

    [RelayCommand(CanExecute = nameof(CanGenerateMappedReport))]
    private async Task GenerateMappedReportAsync()
    {
        if (IsMappingEditOnly)
        {
            var editSourceKey = _editOnlySourceKey;
            if (string.IsNullOrWhiteSpace(editSourceKey))
                return;

            var mappingsToSave = MappingItems
                .Where(x => !string.IsNullOrWhiteSpace(x.Subject))
                .Select(x => new SubjectMappingEntry
                {
                    Subject = x.Subject.Trim(),
                    Include = x.Include,
                    Tag = string.IsNullOrWhiteSpace(x.Tag) ? null : x.Tag.Trim(),
                })
                .ToList();

            _subjectMappings.SetForSourceKey(editSourceKey, mappingsToSave);
            SaveSubjectMappings();
            IsMappingPageOpen = false;
            IsMappingEditOnly = false;
            _editOnlySourceKey = null;
            return;
        }

        if (_pendingOrchestrator is null
            || _pendingExtractedEvents is null
            || _pendingRange is null
            || _pendingFilters is null
            || string.IsNullOrWhiteSpace(_pendingSourceKey))
        {
            return;
        }

        IsBusy = true;
        ShowResult = false;
        ErrorMessage = string.Empty;

        try
        {
            var mappings = MappingItems
                .Where(x => !string.IsNullOrWhiteSpace(x.Subject))
                .Select(x => new SubjectMappingEntry
                {
                    Subject = x.Subject.Trim(),
                    Include = x.Include,
                    Tag = string.IsNullOrWhiteSpace(x.Tag) ? null : x.Tag.Trim(),
                })
                .ToList();

            _subjectMappings.SetForSourceKey(_pendingSourceKey, mappings);
            SaveSubjectMappings();

            var result = await _pendingOrchestrator.GenerateAsync(
                _pendingExtractedEvents,
                _pendingRange,
                OutputFolder,
                _pendingFormat,
                _pendingFilters,
                SubjectTemplates.ToList(),
                mappings,
                WeeklyWorkingHours);

            MeetingCount       = result.EventCount;
            TotalHours         = result.TotalHours;
            WeekNumber         = result.Week.WeekNumber;
            DetailPath         = result.DetailPath;
            SummaryPath        = result.SummaryPath;
            ResultPeriodStart  = result.Week.Start.ToString("dd/MM/yyyy");
            ResultPeriodEnd    = result.Week.End.AddDays(-1).ToString("dd/MM/yyyy");
            ShowResult         = true;
            IsMappingPageOpen  = false;
            IsMappingEditOnly  = false;
            ErrorMessage       = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = string.Format(AppStrings.GenericErrorFormat, ex.Message);
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

    private bool CanGenerateMappedReport() => !IsBusy
        && IsMappingPageOpen
        && (
            (IsMappingEditOnly && !string.IsNullOrWhiteSpace(_editOnlySourceKey))
            || (
                _pendingOrchestrator is not null
                && _pendingExtractedEvents is not null
                && _pendingRange is not null
                && _pendingFilters is not null
                && !string.IsNullOrWhiteSpace(_pendingSourceKey)
            )
        );

    [RelayCommand]
    private void NuovoReport()
    {
        ShowResult   = false;
        IsMappingPageOpen = false;
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
        var textToCopy = !string.IsNullOrEmpty(AuthErrorMessage)
            ? AuthErrorMessage
            : ErrorMessage;

        if (!string.IsNullOrEmpty(textToCopy))
            await Clipboard.Default.SetTextAsync(textToCopy);
    }


    /// <summary>
    /// Restituisce la parent window/VC per presentare la UI di autenticazione MSAL.
    /// </summary>
    private static object? GetPlatformParentWindow()
    {
    #if IOS
        // Ottiene il UIViewController root dalla finestra corrente MAUI
        var window = Application.Current?.Windows.FirstOrDefault();
        var platformWindow = window?.Handler?.PlatformView as UIKit.UIWindow;
        return platformWindow?.RootViewController;
    #elif MACCATALYST
        // Su Mac Catalyst con MSAL system browser usiamo redirect loopback e nessuna parent window.
        return null;
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

    private static bool HasFilterValues(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        return raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Length > 0;
    }

    private static ObservableCollection<string> LoadSubjectTemplates()
    {
        var raw = Preferences.Default.Get("SubjectTemplates", string.Empty);
        if (!string.IsNullOrEmpty(raw))
            return new ObservableCollection<string>(
                raw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        // Migration: read legacy single-template preference
        var legacy = Preferences.Default.Get("SubjectTemplate", string.Empty);
        if (!string.IsNullOrEmpty(legacy))
            return new ObservableCollection<string>([legacy]);

        return new ObservableCollection<string>([CalendarEvent.DefaultTemplate]);
    }

    private void SaveSubjectTemplates()
    {
        Preferences.Default.Set("SubjectTemplates", string.Join("\n", SubjectTemplates));
    }

    private async Task<(ReportOrchestrator Orchestrator, List<CalendarEvent> Events, WeekRange Range, ExportFormat Format, EventFilters Filters, string SourceKey)> ExtractGenerationContextAsync()
    {
        ICalendarSource calendarSource;
        string sourceIdentifier;
        ReportSourceType sourceType;

        if (IsGraphSelected)
        {
            var token = await _authService.GetAccessTokenAsync(GetPlatformParentWindow());
            calendarSource = new CalendarService(token);
            sourceType = ReportSourceType.Graph;

            if (string.IsNullOrWhiteSpace(UserDisplayName) || UserDisplayName == AppStrings.NotAuthenticated)
                UserDisplayName = await _authService.GetUserDisplayNameAsync();

            sourceIdentifier = string.IsNullOrWhiteSpace(UserDisplayName)
                ? "graph-account"
                : UserDisplayName;
        }
        else
        {
            Preferences.Default.Set("IcsUrl", IcsUrl);
            Preferences.Default.Set("SourceGraph", false);

            var downloader = new IcsDownloadService();
            var localPath  = await downloader.DownloadAsync(IcsUrl);
            calendarSource = new IcsCalendarService(localPath);
            sourceType = ReportSourceType.Ics;
            sourceIdentifier = IcsUrl;
        }

        var sourceKey = SourceKeyHasher.Compute(sourceType, sourceIdentifier);
        var range = PeriodSelection switch
        {
            1 => WeekRange.FromPeriod(WeekPeriod.LastWeek, IsWorkWeek),
            2 => WeekRange.FromCustom(CustomStartDate, CustomEndDate),
            _ => WeekRange.FromPeriod(WeekPeriod.ThisWeek, IsWorkWeek),
        };

        var format = IsXlsxSelected ? ExportFormat.Xlsx : ExportFormat.Csv;
        var filters = BuildFilters();
        var events = await calendarSource.GetEventsAsync(range);
        var orchestrator = new ReportOrchestrator(calendarSource);

        return (orchestrator, events, range, format, filters, sourceKey);
    }

    private EventFilters BuildFilters()
    {
        static string[] ParseList(string raw) =>
            raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new EventFilters
        {
            ExcludeTentative = ExcludeTentative,
            Categories = ParseList(ExcludedCategories),
            Clients    = ParseList(ExcludedClients),
            Projects   = ParseList(ExcludedProjects),
            Topics     = ParseList(ExcludedTopics),
        };
    }

    private ObservableCollection<MeetingMappingItemViewModel> BuildMappingItems(
        IEnumerable<string> subjects,
        string sourceKey)
    {
        var existing = _subjectMappings
            .GetForSourceKey(sourceKey)
            .GroupBy(x => x.Subject.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Last(), StringComparer.OrdinalIgnoreCase);

        var normalizedSubjects = subjects
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase);

        var rows = normalizedSubjects.Select(subject =>
        {
            if (existing.TryGetValue(subject, out var mapped))
            {
                return new MeetingMappingItemViewModel
                {
                    Subject = subject,
                    Include = mapped.Include,
                    Tag = mapped.Tag ?? string.Empty,
                };
            }

            return new MeetingMappingItemViewModel
            {
                Subject = subject,
                Include = true,
                Tag = string.Empty,
            };
        });

        return new ObservableCollection<MeetingMappingItemViewModel>(rows);
    }

    private List<string> GetSubjectsWithoutTag(IEnumerable<CalendarEvent> events, string sourceKey)
    {
        var existing = _subjectMappings
            .GetForSourceKey(sourceKey)
            .GroupBy(x => x.Subject.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Last(), StringComparer.OrdinalIgnoreCase);

        return events
            .Select(x => (x.Subject ?? string.Empty).Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(subject =>
            {
                if (!existing.TryGetValue(subject, out var mapped))
                    return true;

                if (!mapped.Include)
                    return false;

                return string.IsNullOrWhiteSpace(mapped.Tag);
            })
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<CalendarEvent> ApplyExclusionsForMapping(
        IEnumerable<CalendarEvent> events,
        EventFilters filters)
    {
        if (filters.IsEmpty)
            return events.ToList();

        var excludeTentative = filters.ExcludeTentative;
        var categories = filters.Categories
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToArray();
        var clients = filters.Clients
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToArray();
        var projects = filters.Projects
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToArray();
        var topics = filters.Topics
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToArray();

        return events.Where(e =>
            (!excludeTentative || !e.IsTentative) &&
            (categories.Length == 0 || !MatchesAnyFilter(e.Category, categories)) &&
            (clients.Length == 0 || !MatchesAnyFilter(e.Client, clients)) &&
            (projects.Length == 0 || !MatchesAnyFilter(e.Project, projects)) &&
            (topics.Length == 0 || !MatchesAnyFilter(e.Topic ?? e.Subject, topics))
        ).ToList();
    }

    private static bool MatchesAnyFilter(string? value, string[] filters)
    {
        if (string.IsNullOrWhiteSpace(value) || filters.Length == 0)
            return false;

        var normalized = value.Trim();
        return filters.Any(filter =>
            normalized.Equals(filter, StringComparison.OrdinalIgnoreCase)
            || normalized.Contains(filter, StringComparison.OrdinalIgnoreCase));
    }

    private SubjectMappingCollection LoadSubjectMappings()
    {
        var raw = Preferences.Default.Get(SubjectMappingsPreferenceKey, string.Empty);
        if (string.IsNullOrWhiteSpace(raw))
            return new SubjectMappingCollection();

        try
        {
            return JsonSerializer.Deserialize<SubjectMappingCollection>(raw) ?? new SubjectMappingCollection();
        }
        catch
        {
            return new SubjectMappingCollection();
        }
    }

    private void SaveSubjectMappings()
    {
        var raw = JsonSerializer.Serialize(_subjectMappings);
        Preferences.Default.Set(SubjectMappingsPreferenceKey, raw);
    }

    private async Task<string?> ResolveCurrentSourceKeyAsync()
    {
        if (IsGraphSelected)
        {
            if (!IsAuthenticated)
            {
                ErrorMessage = AppStrings.AuthenticateToEditTags;
                return null;
            }

            if (string.IsNullOrWhiteSpace(UserDisplayName) || UserDisplayName == AppStrings.NotAuthenticated)
            {
                UserDisplayName = await _authService.GetUserDisplayNameAsync();
            }

            if (string.IsNullOrWhiteSpace(UserDisplayName) || UserDisplayName == AppStrings.NotAuthenticated)
            {
                ErrorMessage = AppStrings.AuthenticateToEditTags;
                return null;
            }

            return SourceKeyHasher.Compute(ReportSourceType.Graph, UserDisplayName);
        }

        if (string.IsNullOrWhiteSpace(IcsUrl))
        {
            ErrorMessage = AppStrings.ConfigureIcsToEditTags;
            return null;
        }

        return SourceKeyHasher.Compute(ReportSourceType.Ics, IcsUrl);
    }
}
