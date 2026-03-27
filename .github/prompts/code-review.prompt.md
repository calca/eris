---
description: 'Analizza e fai review del codice nel progetto eris'
---

# Code Review

Analizza il codice specificato e fornisci una review dettagliata.

## Checklist di Review

### Correttezza
- [ ] La logica è corretta e gestisce tutti i casi limite?
- [ ] Nullable reference types sono gestiti correttamente?
- [ ] Le operazioni async non bloccano mai con `.Result` o `.Wait()`?

### Architettura
- [ ] Rispetta la separazione Core / CLI / UI?
- [ ] Le dipendenze vanno nella direzione corretta (CLI/UI → Core, mai il contrario)?
- [ ] I servizi usano constructor injection?
- [ ] Le interfacce sono segregate correttamente?

### Convenzioni
- [ ] PascalCase per membri pubblici, camelCase per privati?
- [ ] Suffissi corretti (Service, ViewModel, Converter)?
- [ ] `sealed` su classi non ereditabili?
- [ ] File-scoped namespace?
- [ ] Stringhe UI in `AppStrings.resx`?

### Sicurezza
- [ ] Nessun segreto hardcoded (client secrets, token)?
- [ ] Input esterno validato ai confini?
- [ ] Token e credenziali gestiti tramite MSAL/DPAPI?

### Performance
- [ ] Nessuna allocazione inutile in hot path?
- [ ] Paginazione per chiamate API che possono tornare molti risultati?
- [ ] Caching dove appropriato (es. `IcsDownloadService` ha cache 5 min)?

### Test
- [ ] Ci sono test per la nuova logica?
- [ ] I test coprono casi limite e filtri case-insensitive?

## Codice da analizzare

File o area: {{target}}
