---
applyTo: "eris/Core/Services/**"
---

# Istruzioni per Core Services

Quando lavori sui file in `eris/Core/Services/`:

## Regole

- Ogni servizio deve implementare un'interfaccia (`ICalendarSource`, `IExportService`, o una nuova interfaccia dedicata)
- Usa constructor injection per le dipendenze
- Tutti i metodi I/O devono essere `async Task<T>` — mai bloccare con `.Result` o `.Wait()`
- La classe deve essere `sealed` a meno che non sia esplicitamente progettata per ereditarietà
- File-scoped namespace: `namespace eris.Core.Services;`
- Suffisso `Service` nel nome classe (es. `CsvExportService`, `CalendarService`)

## Pattern async

```csharp
// ✅ Corretto
public async Task<IReadOnlyList<CalendarEvent>> GetEventsAsync(DateTimeOffset start, DateTimeOffset end)
{
    var events = await _source.GetEventsAsync(start, end);
    return events;
}

// ❌ Mai fare questo
public IReadOnlyList<CalendarEvent> GetEvents(DateTimeOffset start, DateTimeOffset end)
{
    return _source.GetEventsAsync(start, end).Result; // PROIBITO
}
```

## Dopo ogni modifica

1. Verifica che il Core compili: `dotnet build eris/Core/Core.csproj`
2. Esegui i test: `dotnet test eris/Core.Tests/Core.Tests.csproj`
3. Verifica che la solution completa compili: `dotnet build eris/eris.sln`
