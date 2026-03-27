---
applyTo: "eris/Core/Models/**"
---

# Istruzioni per Core Models

Quando lavori sui file in `eris/Core/Models/`:

## Regole

- Preferisci `record` per DTO/value objects immutabili
- Usa `sealed class` con init-only properties per modelli più complessi
- File-scoped namespace: `namespace eris.Core.Models;`
- Proprietà con PascalCase
- Nullability: gestisci sempre i tipi nullable con `?` dove appropriato

## Pattern preferiti

```csharp
// DTO immutabile — usa record
public record CategorySummary(string Category, string Client, string Project, double Hours);

// Modello con logica — usa sealed class
public sealed class CalendarEvent
{
    public string Subject { get; init; } = "";
    public DateTimeOffset Start { get; init; }
    public DateTimeOffset End { get; init; }
    public double DurationHours => (End - Start).TotalHours;
    
    public void ParseStructuredSubject(string template) { ... }
}
```

## Attenzione speciale

- `CalendarEvent.ParseStructuredSubject()` è il cuore del parsing strutturato "Client | Project | Topic"
- `WeekRange` gestisce boundary anno (dicembre → gennaio) con ISO 8601
- `EventFilters` usa partial matching case-insensitive
