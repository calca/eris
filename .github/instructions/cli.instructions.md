---
applyTo: "eris/CLI/**"
---

# Istruzioni per CLI

Quando lavori sul CLI di eris:

## Stack

- **System.CommandLine** 2.0 beta: `RootCommand`, `Command`, `Option<T>`
- **Spectre.Console**: output ricco nel terminale
- **Top-level statements**: `Program.cs` senza classe

## Regole

- Opzioni CLI in `--kebab-case` con alias breve `-x`
- Handler tutti asincroni: `SetHandler(async (args) => { ... })`
- Output con Spectre.Console markup: `[green]OK[/]`, `[red]Error[/]`
- Errori gestiti con try/catch, mai crash non gestiti
- Configurazione da `ConfigLoader.Load()` + override da opzioni CLI
- Non duplicare logica di business — usa servizi Core

## Pattern comando

```csharp
var myCommand = new Command("my-command", "Descrizione del comando");
var optionName = new Option<string>(["--name", "-n"], "Nome dell'elemento");
myCommand.AddOption(optionName);
myCommand.SetHandler(async (name) =>
{
    try
    {
        var config = ConfigLoader.Load();
        // ... usa servizi Core ...
        AnsiConsole.MarkupLine("[green]Operazione completata![/]");
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLine($"[red]Errore: {ex.Message}[/]");
    }
}, optionName);
rootCommand.AddCommand(myCommand);
```
