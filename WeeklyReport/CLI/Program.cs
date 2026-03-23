using System.CommandLine;
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
    var appConfig   = ConfigLoader.Load(config);
    var auth        = new GraphAuthService(appConfig);
    var orchestrator = new ReportOrchestrator(auth);
    var period      = week.Equals("last", StringComparison.OrdinalIgnoreCase)
                        ? WeekPeriod.LastWeek
                        : WeekPeriod.ThisWeek;

    ReportResult result = null!;

    await AnsiConsole.Status()
        .Spinner(Spinner.Known.Dots)
        .SpinnerStyle(Style.Parse("skyblue1"))
        .StartAsync("[skyblue1]Generazione report in corso…[/]", async ctx =>
        {
            result = await orchestrator.GenerateAsync(period, output, msg =>
            {
                ctx.Status($"[yellow]{Markup.Escape(msg)}[/]");
                AnsiConsole.MarkupLine($"\n[yellow]{Markup.Escape(msg)}[/]");
            });
        });

    var table = new Table()
        .Border(TableBorder.Rounded)
        .BorderStyle(Style.Parse("grey"))
        .AddColumn("[grey50]Campo[/]")
        .AddColumn("[white]Valore[/]");

    table.AddRow("Settimana",   Markup.Escape(result.Week.DisplayName));
    table.AddRow("Meeting",     result.EventCount.ToString());
    table.AddRow("Ore totali",  $"{result.TotalHours:F1} h");
    table.AddRow("Detail CSV",  Markup.Escape(result.DetailCsvPath));
    table.AddRow("Summary CSV", Markup.Escape(result.SummaryCsvPath));

    AnsiConsole.Write(table);
    AnsiConsole.MarkupLine("\n[bold green]✓ Report generato con successo![/]");

}, weekOpt, outputOpt, configOpt);

// ── whoami ─────────────────────────────────────────────────────────────────────
var whoamiCmd = new Command("whoami", "Mostra l'account Microsoft attualmente autenticato");

whoamiCmd.SetHandler(async () =>
{
    var appConfig = ConfigLoader.Load(null);
    var auth      = new GraphAuthService(appConfig);
    var name      = await auth.GetUserDisplayNameAsync();

    if (string.IsNullOrWhiteSpace(name))
        AnsiConsole.MarkupLine("[yellow]Nessun account autenticato. Usa 'generate' per effettuare il login.[/]");
    else
        AnsiConsole.MarkupLine($"[green]Account autenticato:[/] {Markup.Escape(name)}");
});

rootCommand.AddCommand(generateCmd);
rootCommand.AddCommand(whoamiCmd);

return await rootCommand.InvokeAsync(args);
