---
description: 'Debug di un problema: analisi, diagnosi e fix'
---

# Debug Issue

Analizza e risolvi il problema descritto.

## Processo di Debug

1. **Riproduci**: comprendi il problema e come riprodurlo
2. **Localizza**: trova il codice responsabile
3. **Diagnostica**: identifica la root cause
4. **Fix**: applica la correzione minima necessaria
5. **Verifica**: esegui i test per confermare il fix
6. **Regression**: aggiungi un test che cattura il bug

## Comandi utili

```bash
# Build completo
dotnet build eris/eris.sln

# Test
dotnet test eris/Core.Tests/Core.Tests.csproj

# Build CLI
DOTNET_ROOT="$HOME/.dotnet-local" "$HOME/.dotnet-local/dotnet" build eris/CLI/CLI.csproj

# Build UI Mac
DOTNET_ROOT="$HOME/.dotnet-local" "$HOME/.dotnet-local/dotnet" build eris/UI/UI.csproj -f net10.0-maccatalyst
```

## Aree comuni di problemi

- **Parsing soggetto**: `CalendarEvent.ParseStructuredSubject()` — template matching
- **Filtri**: `ReportOrchestrator` — case-insensitive, partial matching
- **Auth MSAL**: `GraphAuthService` — token cache, device code flow su Mac
- **ICS parsing**: `IcsCalendarService` — ricorrenze, timezone, formati data
- **Export**: encoding UTF-8 BOM, formattazione italiana, separatore punto e virgola

## Problema da risolvere

Descrizione: {{problem}}
Messaggio errore: {{error}}
