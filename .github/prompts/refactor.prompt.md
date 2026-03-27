---
description: 'Refactoring sicuro con preservazione dei test e delle interfacce'
---

# Refactoring

Esegui un refactoring del codice specificato, garantendo che:

## Regole di Refactoring

1. **Non rompere le interfacce pubbliche**: `ICalendarSource`, `IExportService` e i loro contratti
2. **Mantieni i test verdi**: esegui `dotnet test eris/Core.Tests/Core.Tests.csproj` dopo ogni modifica
3. **Preserva il comportamento**: stessi input → stessi output
4. **Segui le convenzioni eris**:
   - File-scoped namespace
   - `sealed` class dove appropriato
   - Constructor injection
   - Async pipeline completa
5. **Aggiorna i test** se la signature interna cambia
6. **Non toccare** codice non correlato al refactoring richiesto

## Strategia

1. Leggi e comprendi il codice attuale
2. Identifica le dipendenze (chi usa questo codice?)
3. Applica le modifiche incrementalmente
4. Verifica con `dotnet build eris/eris.sln` dopo ogni step
5. Esegui i test alla fine

## Target del refactoring

Area: {{area}}
Obiettivo: {{goal}}
