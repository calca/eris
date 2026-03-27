---
description: 'Aggiungi un nuovo comando CLI con System.CommandLine + Spectre.Console'
---

# Nuovo Comando CLI

Aggiungi un nuovo comando al CLI di eris in `eris/CLI/Program.cs`.

## Architettura CLI

- **System.CommandLine**: definizione comandi con `Command`, `Option<T>`, `Argument<T>`
- **Spectre.Console**: output ricco (tabelle, colori, progress bar, figlet)
- **Entry point**: top-level statements in `Program.cs`
- **DI**: servizi Core iniettati tramite costruttore

## Convenzioni

1. Ogni comando è un `Command` aggiunto al `RootCommand`
2. Opzioni con `--kebab-case` e alias breve `-x`
3. Handler asincrono che usa servizi Core
4. Output con Spectre.Console markup `[green]...[/]`
5. Gestione errori con `try/catch` e messaggio colorato `[red]`
6. Configurazione da `AppConfig` (caricato da `ConfigLoader`)

## Comando da creare

Nome: {{name}}
Descrizione: {{description}}
Opzioni: {{options}}
