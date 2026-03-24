using System.CommandLine;
using Microsoft.Identity.Client;
using OutlookWeeklyReport.Core.Models;
using OutlookWeeklyReport.Core.Services;
using Spectre.Console;

// ── Banner ─────────────────────────────────────────────────────────────────────
AnsiConsole.Write(new FigletText("OWReport").Color(Color.SkyBlue1));
AnsiConsole.MarkupLine("[bold skyblue1]Outlook Weekly Meeting Report[/]\n");

// ── Root command ───────────────────────────────────────────────────────────────
var rootCommand = new RootCommand("Outlook Weekly Meeting Report — esporta i meeting accettati in CSV settimanale");

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

generateCmd.AddOption(weekOpt);
generateCmd.AddOption(outputOpt);
generateCmd.AddOption(configOpt);

generateCmd.SetHandler(async (string week, string output, string? config) =>
{
    await RunGenerateAsync(week, output, config);

}, weekOpt, outputOpt, configOpt);

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
    var pca = PublicClientApplicationBuilder
        .Create(appConfig.ClientId)
        .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
        .WithRedirectUri("http://localhost")
        .Build();

    return (appConfig, new GraphAuthService(pca, appConfig.Scopes));
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

                    await RunGenerateAsync(week, output, configPath);
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

async Task RunGenerateAsync(string week, string output, string? config)
{
    var (_, auth) = BuildAuthService(config);
    var orchestrator = new ReportOrchestrator(auth);
    var period = week.Equals("last", StringComparison.OrdinalIgnoreCase)
        ? WeekPeriod.LastWeek
        : WeekPeriod.ThisWeek;

    ReportResult result = null!;

    await AnsiConsole.Status()
        .Spinner(Spinner.Known.Dots)
        .SpinnerStyle(Style.Parse("skyblue1"))
        .StartAsync("[skyblue1]Generazione report in corso…[/]", async _ =>
        {
            result = await orchestrator.GenerateAsync(period, output);
        });

    var table = new Table()
        .Border(TableBorder.Rounded)
        .BorderStyle(Style.Parse("grey"))
        .AddColumn("[grey50]Campo[/]")
        .AddColumn("[white]Valore[/]");

    table.AddRow("Settimana", Markup.Escape(result.Week.DisplayName));
    table.AddRow("Meeting", result.EventCount.ToString());
    table.AddRow("Ore totali", $"{result.TotalHours:F1} h");
    table.AddRow("Detail CSV", Markup.Escape(result.DetailCsvPath));
    table.AddRow("Summary CSV", Markup.Escape(result.SummaryCsvPath));

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
