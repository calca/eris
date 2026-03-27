---
description: 'Genera un nuovo modello in Core/Models/ seguendo le convenzioni eris'
---

# Nuovo Modello Core

Crea un nuovo modello nel progetto **eris.Core** seguendo queste regole:

## Requisiti

1. **File**: crea `{{ModelName}}.cs` in `eris/Core/Models/`
2. **Convenzioni**:
   - File-scoped namespace: `namespace eris.Core.Models;`
   - Preferisci `record` per DTO immutabili, `sealed class` con init-only properties per modelli complessi
   - `#nullable enable` implicito
   - Proprietà con PascalCase
   - Documenta proprietà non ovvie con `///` XML doc
3. **Validazione**: aggiungi validazione nei costruttori/factory solo se il modello è creato da input esterno

## Contesto

Il modello si chiama: {{name}}
I campi sono: {{fields}}
Lo scopo è: {{purpose}}
