---
applyTo: "eris/Core.Tests/**"
---

# Istruzioni per Unit Test

Quando lavori sui test xUnit:

## Convenzioni

- **Naming**: `MethodName_Scenario_ExpectedResult`
- **Pattern**: Arrange-Act-Assert
- **Mock**: usa `FakeCalendarSource` per simulare dati calendario
- **Classe**: `public sealed class`
- **Namespace**: `namespace eris.Core.Tests;`
- **Framework**: xUnit con `[Fact]` e `[Theory]`

## Template

```csharp
namespace eris.Core.Tests;

public sealed class ServiceNameTests
{
    [Fact]
    public async Task GetEvents_WithDateRange_ReturnsFilteredEvents()
    {
        // Arrange
        var events = new List<CalendarEvent>
        {
            new() { Subject = "Acme | Portal | Planning", Start = ..., End = ... }
        };
        var source = new FakeCalendarSource(events);

        // Act
        var result = await source.GetEventsAsync(start, end);

        // Assert
        Assert.Single(result);
        Assert.Equal("Acme", result[0].Client);
    }

    [Theory]
    [InlineData("planning", true)]   // case-insensitive
    [InlineData("PLANNING", true)]
    [InlineData("nonexistent", false)]
    public async Task Filter_TopicMatching_IsCaseInsensitive(string filter, bool shouldExclude)
    {
        // ...
    }
}
```

## Cosa testare sempre

- Filtri case-insensitive e partial matching
- Valori null/empty per Subject, Client, Project, Topic
- Boundary delle date (settimana a cavallo di anno)
- Parsing soggetto strutturato con template diversi
- Esclusione eventi all-day
- Calcolo ore e percentuali
