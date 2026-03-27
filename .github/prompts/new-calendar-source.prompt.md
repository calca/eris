---
description: 'Implementa un nuovo ICalendarSource per una sorgente calendario alternativa'
---

# Nuova Sorgente Calendario

Crea un nuovo servizio che implementa `ICalendarSource` per fornire eventi da una sorgente alternativa.

## Interfaccia da implementare

```csharp
public interface ICalendarSource
{
    Task<IReadOnlyList<CalendarEvent>> GetEventsAsync(DateTimeOffset start, DateTimeOffset end);
}
```

## Convenzioni

1. **File**: `eris/Core/Services/{{Source}}CalendarService.cs`
2. **Namespace**: `namespace eris.Core.Services;`
3. **Classe**: `sealed class` che implementa `ICalendarSource`
4. **Suffisso**: `CalendarService` (es. `GoogleCalendarService`)
5. **Filtraggio**: escludi eventi all-day, gestisci ricorrenze
6. **Parsing soggetto**: chiama `CalendarEvent.ParseStructuredSubject()` per parsing "Client | Project | Topic"
7. **Async completo**: mai bloccare con `.Result` o `.Wait()`

## Riferimenti esistenti

- `CalendarService`: Microsoft Graph API con paginazione
- `IcsCalendarService`: parsing file ICS locale con espansione ricorrenze

## Sorgente da implementare

Nome sorgente: {{source}}
API/protocollo: {{protocol}}
Requisiti: {{requirements}}
