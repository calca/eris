---
description: 'Genera un nuovo servizio in Core/Services/ seguendo le convenzioni eris'
---

# Nuovo Servizio Core

Crea un nuovo servizio nel progetto **eris.Core** seguendo queste regole:

## Requisiti

1. **Interfaccia**: crea `I{{ServiceName}}.cs` in `eris/Core/Services/` con i metodi necessari
2. **Implementazione**: crea `{{ServiceName}}.cs` nella stessa cartella
3. **Convenzioni**:
   - File-scoped namespace: `namespace eris.Core.Services;`
   - Classe `sealed` con constructor injection
   - `#nullable enable` implicito (non serve aggiungerlo)
   - Tutti i metodi I/O devono essere `async Task<T>` o `async Task`
   - Mai bloccare con `.Result` o `.Wait()`
   - Suffisso `Service` obbligatorio nel nome
4. **Test**: crea il test file corrispondente in `eris/Core.Tests/` con naming `{{ServiceName}}Tests.cs`
   - Pattern di naming test: `MethodName_Scenario_ExpectedResult`
   - Usa xUnit

## Contesto del file da creare

Il servizio si chiama: {{name}}
Lo scopo è: {{purpose}}
