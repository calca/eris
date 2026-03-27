---
description: 'Implementa un nuovo IExportService per un formato di export personalizzato'
---

# Nuovo Export Service

Crea un nuovo servizio di esportazione report implementando `IExportService`.

## Interfaccia da implementare

```csharp
public interface IExportService
{
    Task ExportAsync(IEnumerable<CalendarEvent> events, IEnumerable<CategorySummary> summary, string outputPath);
}
```

## Convenzioni

1. **File**: `eris/Core/Services/{{Format}}ExportService.cs`
2. **Namespace**: `namespace eris.Core.Services;`
3. **Classe**: `sealed class` che implementa `IExportService`
4. **Nome**: suffisso `ExportService` (es. `PdfExportService`, `JsonExportService`)
5. **Encoding**: UTF-8 con BOM per compatibilità Excel (se testo)
6. **Locale**: formattazione italiana (virgola decimale, punto migliaia)
7. **Async**: operazioni I/O tutte async
8. **Errori**: lancia `IOException` solo per errori I/O reali

## Riferimenti esistenti

- `CsvExportService`: export con CsvHelper, semicolon delimiter, due file (detail + summary)
- `XlsxExportService`: export con ClosedXML, un file con due fogli (Summary + Detail)

## Formato da implementare

Formato: {{format}}
Requisiti specifici: {{requirements}}
