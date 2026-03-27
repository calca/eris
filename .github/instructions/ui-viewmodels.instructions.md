---
applyTo: "eris/UI/ViewModels/**"
---

# Istruzioni per UI ViewModels

Quando lavori sui ViewModels MAUI:

## MVVM Toolkit

Usa **sempre** gli attributi del CommunityToolkit.Mvvm:

```csharp
// Proprietà osservabile (genera la property pubblica automaticamente)
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(CanGenerate))]
[NotifyCanExecuteChangedFor(nameof(GenerateReportCommand))]
private string outputFolder = "";

// Comando asincrono
[RelayCommand(CanExecute = nameof(CanGenerate))]
private async Task GenerateReport() { ... }
```

## Regole

- **Mai logica nel code-behind** — tutta nel ViewModel
- I field per `[ObservableProperty]` usano camelCase senza `_` prefix
- Cascading updates: usa `[NotifyPropertyChangedFor]` e `[NotifyCanExecuteChangedFor]`
- Comandi asincroni: il metodo deve essere `async Task`, mai `async void`
- Cattura le eccezioni nei comandi e mostra un messaggio all'utente
- La DI è configurata in `MauiProgram.cs`

## Servizi iniettabili

- `AppConfig` — configurazione corrente
- `GraphAuthService` — autenticazione Microsoft
- `IPublicClientApplication` — MSAL client
- `ICalendarSource` — sorgente calendario (Graph o ICS)
- `IExportService` — servizio di export (CSV o XLSX)
- `ReportOrchestrator` — orchestratore report
