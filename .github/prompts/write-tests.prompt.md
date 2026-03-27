---
description: 'Scrivi o migliora unit test xUnit per un componente Core'
---

# Genera Unit Test

Scrivi test xUnit per il componente specificato nel progetto **eris.Core.Tests**.

## Convenzioni

1. **File**: `eris/Core.Tests/{{ComponentName}}Tests.cs`
2. **Namespace**: `namespace eris.Core.Tests;`
3. **Naming**: `MethodName_Scenario_ExpectedResult`
4. **Mock**: usa `FakeCalendarSource` (implementa `ICalendarSource`) per mockare dati calendario
5. **Arrange-Act-Assert**: segui il pattern AAA con commenti separatori
6. **Filtri**: testa sempre case-insensitive e partial matching
7. **Framework**: xUnit, no FluentAssertions (usa Assert.* nativo)
8. **Classe**: `sealed class` con `public` visibility

## Esempio di struttura

```csharp
namespace eris.Core.Tests;

public sealed class {{ComponentName}}Tests
{
    [Fact]
    public async Task MethodName_Scenario_ExpectedResult()
    {
        // Arrange
        var source = new FakeCalendarSource([...]);

        // Act
        var result = await ...;

        // Assert
        Assert.Equal(expected, result);
    }
}
```

## Cosa testare

Componente: {{component}}
Scenari specifici: {{scenarios}}
