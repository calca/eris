using System.CommandLine;
using eris.Core.Models;
using eris.Core.Services;
using Spectre.Console;

// ── Banner ─────────────────────────────────────────────────────────────────────
AnsiConsole.Write(new FigletText("eris").Color(Color.SkyBlue1));
AnsiConsole.MarkupLine("[bold skyblue1]eris[/]\n");

// ── Root command ───────────────────────────────────────────────────────────────
var rootCommand = new RootCommand("eris — esporta i meeting accettati in CSV settimanale");

// ── generate ───────────────────────────────────────────────────────────────────
var generateCmd = new Command("generate", "Genera i file detail.csv e summary.csv per la settimana selezionata");

var weekOpt = new Option<string>(
    "--week",
    getDefaultValue: () => "this",
    description: "Settimana: 'this' (corrente) o 'last' (scorsa)");

var outputOpt = new Option<string>(
    "--output",
    getDefaultValue: () => Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
    description: "Cartella di output (default: Desktop)");

var configOpt = new Option<string?>(
    "--config",
    description: "Percorso opzionale di un appsettings.json personalizzato");

var sourceOpt = new Option<string?>(
    "--source",
    description: "Sorgente calendario: 'graph' o 'ics' (default da config)");

var icsUrlOpt = new Option<string?>(
    "--ics-url",
    description: "URL del file .ics (usato con --source ics)");

var formatOpt = new Option<string>(
    "--format",
    getDefaultValue: () => "xlsx",
    description: "Formato di esportazione: 'csv' o 'xlsx' (default: xlsx)");

var weeklyHoursOpt = new Option<double?>(
    "--weekly-hours",
    description: "Monte ore lavorative settimanali di riferimento (default da config, es. 40 o 30)");

var excludeCategoriesOpt = new Option<string?>(
    "--exclude-categories",
    description: "Categorie da escludere, separate da virgola (es. \"Personale,OOO\"; default da config)");

var excludeClientsOpt = new Option<string?>(
    "--exclude-clients",
    description: "Clienti da escludere, separati da virgola (default da config)");

var excludeProjectsOpt = new Option<string?>(
    "--exclude-projects",
    description: "Progetti da escludere, separati da virgola (default da config)");

var excludeTopicsOpt = new Option<string?>(
    "--exclude-topics",
    description: "Topic da escludere, separati da virgola (default da config)");

generateCmd.AddOption(weekOpt);
generateCmd.AddOption(outputOpt);
generateCmd.AddOption(configOpt);
generateCmd.AddOption(sourceOpt);
generateCmd.AddOption(icsUrlOpt);
generateCmd.AddOption(formatOpt);
generateCmd.AddOption(weeklyHoursOpt);
generateCmd.AddOption(excludeCategoriesOpt);
generateCmd.AddOption(excludeClientsOpt);
generateCmd.AddOption(excludeProjectsOpt);
generateCmd.AddOption(excludeTopicsOpt);

generateCmd.SetHandler(async (System.CommandLine.Invocation.InvocationContext ctx) =>
{
    await RunGenerateAsync(
        ctx.ParseResult.GetValueForOption(weekOpt)!,
        ctx.ParseResult.GetValueForOption(outputOpt)!,
        ctx.ParseResult.GetValueForOption(configOpt),
        ctx.ParseResult.GetValueForOption(sourceOpt),
        ctx.ParseResult.GetValueForOption(icsUrlOpt),
        ctx.ParseResult.GetValueForOption(formatOpt)!,
        ctx.ParseResult.GetValueForOption(weeklyHoursOpt),
        ctx.ParseResult.GetValueForOption(excludeCategoriesOpt),
        ctx.ParseResult.GetValueForOption(excludeClientsOpt),
        ctx.ParseResult.GetValueForOption(excludeProjectsOpt),
        ctx.ParseResult.GetValueForOption(excludeTopicsOpt));
});

// ── whoami ─────────────────────────────────────────────────────────────────────
var whoamiCmd = new Command("whoami", "Mostra l'account Microsoft attualmente autenticato");

whoamiCmd.SetHandler(RunWhoAmIAsync);

// ── test-login ────────────────────────────────────────────────────────────────
var loginCmd = new Command("test-login", "Esegue solo il login Microsoft e verifica l'accesso a Graph");
loginCmd.AddAlias("login");
var loginConfigOpt = new Option<string?>(
    "--config",
    description: "Percorso opzionale di un appsettings.json personalizzato");
loginCmd.AddOption(loginConfigOpt);
loginCmd.SetHandler(async (string? config) =>
{
    await RunLoginTestAsync(config);
}, loginConfigOpt);

// ── signout ────────────────────────────────────────────────────────────────────
var signoutCmd = new Command("signout", "Rimuove gli account salvati nella cache locale di MSAL");

signoutCmd.SetHandler(async () =>
{
    var (_, auth) = BuildAuthService(null);
    await auth.SignOutAsync();
    AnsiConsole.MarkupLine("[green]Cache locale ripulita. Il prossimo accesso richiederà il login.[/]");
});

// ── interactive ────────────────────────────────────────────────────────────────
var interactiveCmd = new Command("interactive", "Avvia una procedura guidata interattiva");
interactiveCmd.AddAlias("i");
interactiveCmd.SetHandler(RunInteractiveAsync);

rootCommand.AddCommand(generateCmd);
rootCommand.AddCommand(whoamiCmd);
rootCommand.AddCommand(loginCmd);
rootCommand.AddCommand(signoutCmd);
rootCommand.AddCommand(interactiveCmd);

if (args.Length == 0)
    return await RunInteractiveAsync();

return await rootCommand.InvokeAsync(args);

// ── Helpers ────────────────────────────────────────────────────────────────────

(AppConfig Config, GraphAuthService Auth) BuildAuthService(string? configPath)
{
    var appConfig = ConfigLoader.Load(configPath);

    try
    {
        var pca = new GraphAuthClientFactory().Create(appConfig);
        return (appConfig, new GraphAuthService(pca, appConfig.Scopes));
    }
    catch (InvalidOperationException ex) when (
        ex.Message.Contains("AzureAd:ClientId", StringComparison.OrdinalIgnoreCase) ||
        ex.Message.Contains("AzureAd:TenantId", StringComparison.OrdinalIgnoreCase))
    {
        var configSource = string.IsNullOrWhiteSpace(configPath) ? "appsettings.json" : configPath;
        throw new InvalidOperationException(
            $"Configurazione Azure AD non valida in '{configSource}'. {ex.Message}",
            ex);
    }
}

async Task<int> RunInteractiveAsync()
{
    AnsiConsole.MarkupLine("[bold skyblue1]Modalità interattiva CLI[/]\n");

    while (true)
    {
        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Seleziona un'azione")
                .AddChoices("Genera report", "Test login", "Mostra account", "Logout locale", "Esci"));

        try
        {
            switch (action)
            {
                case "Genera report":
                    var sourceChoice = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("Sorgente calendario")
                            .AddChoices("ICS (file .ics)", "Graph (Microsoft Outlook)"));

                    string? icsUrlInteractive = null;
                    if (sourceChoice.StartsWith("ICS"))
                    {
                        icsUrlInteractive = AnsiConsole.Ask<string>("URL del file .ics");
                    }

                    var week = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("Settimana da esportare")
                            .AddChoices("this", "last"));

                    var defaultOutput = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    var output = AnsiConsole.Ask("Cartella output", defaultOutput);
                    if (!Directory.Exists(output))
                    {
                        var createFolder = AnsiConsole.Confirm($"La cartella '{output}' non esiste. Vuoi crearla?", true);
                        if (createFolder)
                            Directory.CreateDirectory(output);
                        else
                        {
                            AnsiConsole.MarkupLine("[yellow]Operazione annullata.[/]");
                            break;
                        }
                    }

                    string? configPath = null;
                    if (AnsiConsole.Confirm("Vuoi usare un appsettings.json personalizzato?", false))
                    {
                        configPath = AnsiConsole.Ask<string>("Percorso file config");
                        if (!File.Exists(configPath))
                        {
                            AnsiConsole.MarkupLine($"[red]File non trovato:[/] {Markup.Escape(configPath)}");
                            break;
                        }
                    }

                    var formatChoice = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("Formato di esportazione")
                            .AddChoices("XLSX", "CSV"));

                    double? weeklyHoursInteractive = null;
                    if (AnsiConsole.Confirm("Vuoi specificare le ore lavorative settimanali?", false))
                    {
                        weeklyHoursInteractive = AnsiConsole.Ask<double>("Ore settimanali", 40);
                    }

                    string? excludeCategoriesInteractive = null;
                    string? excludeClientsInteractive    = null;
                    string? excludeProjectsInteractive   = null;
                    string? excludeTopicsInteractive     = null;
                    if (AnsiConsole.Confirm("Vuoi escludere eventi per categoria, cliente, progetto o topic?", false))
                    {
                        excludeCategoriesInteractive = AnsiConsole.Ask<string>("Categorie da escludere (separate da virgola, invio per saltare)", string.Empty);
                        excludeClientsInteractive    = AnsiConsole.Ask<string>("Clienti da escludere (separati da virgola, invio per saltare)",    string.Empty);
                        excludeProjectsInteractive   = AnsiConsole.Ask<string>("Progetti da escludere (separati da virgola, invio per saltare)",   string.Empty);
                        excludeTopicsInteractive     = AnsiConsole.Ask<string>("Topic da escludere (separati da virgola, invio per saltare)",      string.Empty);
                    }

                    await RunGenerateAsync(week, output, configPath,
                        sourceChoice.StartsWith("ICS") ? "ics" : "graph",
                        icsUrlInteractive,
                        formatChoice,
                        weeklyHoursInteractive,
                        string.IsNullOrWhiteSpace(excludeCategoriesInteractive) ? null : excludeCategoriesInteractive,
                        string.IsNullOrWhiteSpace(excludeClientsInteractive)    ? null : excludeClientsInteractive,
                        string.IsNullOrWhiteSpace(excludeProjectsInteractive)   ? null : excludeProjectsInteractive,
                        string.IsNullOrWhiteSpace(excludeTopicsInteractive)     ? null : excludeTopicsInteractive);
                    break;

                case "Mostra account":
                    await RunWhoAmIAsync();
                    break;

                case "Test login":
                    string? loginConfigPath = null;
                    if (AnsiConsole.Confirm("Vuoi usare un appsettings.json personalizzato?", false))
                    {
                        loginConfigPath = AnsiConsole.Ask<string>("Percorso file config");
                        if (!File.Exists(loginConfigPath))
                        {
                            AnsiConsole.MarkupLine($"[red]File non trovato:[/] {Markup.Escape(loginConfigPath)}");
                            break;
                        }
                    }

                    await RunLoginTestAsync(loginConfigPath);
                    break;

                case "Logout locale":
                    var (_, auth) = BuildAuthService(null);
                    await auth.SignOutAsync();
                    AnsiConsole.MarkupLine("[green]Logout locale completato.[/]");
                    break;

                case "Esci":
                    return 0;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Errore:[/] {Markup.Escape(ex.Message)}");
        }

        AnsiConsole.WriteLine();
    }
}

async Task RunGenerateAsync(string week, string output, string? config, string? source = null, string? icsUrl = null, string format = "xlsx", double? weeklyHours = null, string? excludeCategories = null, string? excludeClients = null, string? excludeProjects = null, string? excludeTopics = null)
{
    var appConfig = ConfigLoader.Load(config);

    // Argomento CLI --weekly-hours sovrascrive il valore da config
    if (weeklyHours is { } wh)
    {
        if (wh > 0)
            appConfig.WeeklyWorkingHours = wh;
        else
            AnsiConsole.MarkupLine($"[yellow]Avviso: --weekly-hours deve essere un valore positivo. Verrà usato il valore da config ({appConfig.WeeklyWorkingHours:F0} h).[/]");
    }

    // Argomenti CLI --exclude-* sovrascrivono i valori da config
    static string[] ParseList(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
            ? []
            : raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    if (excludeCategories != null) appConfig.Filters.Categories = ParseList(excludeCategories);
    if (excludeClients    != null) appConfig.Filters.Clients    = ParseList(excludeClients);
    if (excludeProjects   != null) appConfig.Filters.Projects   = ParseList(excludeProjects);
    if (excludeTopics     != null) appConfig.Filters.Topics     = ParseList(excludeTopics);

    // Determina la sorgente: argomento CLI > config
    var sourceType = appConfig.SourceType;
    if (!string.IsNullOrWhiteSpace(source))
        sourceType = source.Equals("ics", StringComparison.OrdinalIgnoreCase)
            ? ReportSourceType.Ics
            : ReportSourceType.Graph;

    var effectiveIcsUrl = icsUrl ?? appConfig.IcsUrl;

    ICalendarSource calendarSource;

    if (sourceType == ReportSourceType.Ics)
    {
        if (string.IsNullOrWhiteSpace(effectiveIcsUrl))
        {
            AnsiConsole.MarkupLine("[red]Errore: URL ICS non specificato. Usa --ics-url o configura Source:IcsUrl in appsettings.json.[/]");
            return;
        }

        var downloader = new IcsDownloadService();
        string localPath;

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("skyblue1"))
            .StartAsync("[skyblue1]Download file ICS…[/]", async _ =>
            {
                localPath = await downloader.DownloadAsync(effectiveIcsUrl);
            });

        var finalPath = await downloader.DownloadAsync(effectiveIcsUrl);
        AnsiConsole.MarkupLine($"[grey]File ICS: {Markup.Escape(finalPath)}[/]");
        calendarSource = new IcsCalendarService(finalPath);
    }
    else
    {
        var (_, auth) = BuildAuthService(config);
        var token = await auth.GetAccessTokenAsync();
        calendarSource = new CalendarService(token);
    }

    var orchestrator = new ReportOrchestrator(calendarSource);
    var period = week.Equals("last", StringComparison.OrdinalIgnoreCase)
        ? WeekPeriod.LastWeek
        : WeekPeriod.ThisWeek;

    var exportFormat = format.Equals("csv", StringComparison.OrdinalIgnoreCase)
        ? ExportFormat.Csv
        : ExportFormat.Xlsx;

    ReportResult result = null!;

    await AnsiConsole.Status()
        .Spinner(Spinner.Known.Dots)
        .SpinnerStyle(Style.Parse("skyblue1"))
        .StartAsync("[skyblue1]Generazione report in corso…[/]", async _ =>
        {
            result = await orchestrator.GenerateAsync(period, output, exportFormat, appConfig.Filters, null, appConfig.WeeklyWorkingHours);
        });

    var table = new Table()
        .Border(TableBorder.Rounded)
        .BorderStyle(Style.Parse("grey"))
        .AddColumn("[grey50]Campo[/]")
        .AddColumn("[white]Valore[/]");

    table.AddRow("Settimana", Markup.Escape(result.Week.DisplayName));
    table.AddRow("Sorgente", sourceType.ToString());
    table.AddRow("Formato", exportFormat.ToString().ToUpper());
    table.AddRow("Ore settimanali", $"{appConfig.WeeklyWorkingHours:F0} h");
    if (appConfig.Filters.Categories.Length > 0)
        table.AddRow("Escluse categorie", Markup.Escape(string.Join(", ", appConfig.Filters.Categories)));
    if (appConfig.Filters.Clients.Length > 0)
        table.AddRow("Esclusi clienti",   Markup.Escape(string.Join(", ", appConfig.Filters.Clients)));
    if (appConfig.Filters.Projects.Length > 0)
        table.AddRow("Esclusi progetti",  Markup.Escape(string.Join(", ", appConfig.Filters.Projects)));
    if (appConfig.Filters.Topics.Length > 0)
        table.AddRow("Esclusi topic",     Markup.Escape(string.Join(", ", appConfig.Filters.Topics)));
    table.AddRow("Meeting", result.EventCount.ToString());
    table.AddRow("Ore totali", $"{result.TotalHours:F1} h");
    table.AddRow("Detail", Markup.Escape(result.DetailPath));
    table.AddRow("Summary", Markup.Escape(result.SummaryPath));

    AnsiConsole.Write(table);
    AnsiConsole.MarkupLine("\n[bold green]✓ Report generato con successo![/]");
}

async Task RunWhoAmIAsync()
{
    var (_, auth) = BuildAuthService(null);
    var name = await auth.GetUserDisplayNameAsync();

    if (string.IsNullOrWhiteSpace(name))
        AnsiConsole.MarkupLine("[yellow]Nessun account autenticato. Usa 'generate' per effettuare il login.[/]");
    else
        AnsiConsole.MarkupLine($"[green]Account autenticato:[/] {Markup.Escape(name)}");
}

async Task RunLoginTestAsync(string? config)
{
    var (_, auth) = BuildAuthService(config);

    await AnsiConsole.Status()
        .Spinner(Spinner.Known.Dots)
        .SpinnerStyle(Style.Parse("skyblue1"))
        .StartAsync("[skyblue1]Login Microsoft in corso…[/]", async _ =>
        {
            await auth.GetAccessTokenAsync();
        });

    var name = await auth.GetUserDisplayNameAsync();
    if (string.IsNullOrWhiteSpace(name))
        AnsiConsole.MarkupLine("[green]Login riuscito.[/] Token ottenuto correttamente.");
    else
        AnsiConsole.MarkupLine($"[green]Login riuscito.[/] Account: {Markup.Escape(name)}");
}
