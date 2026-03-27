## Descrizione

<!-- Descrivi brevemente cosa fa questa PR -->

## Tipo di modifica

- [ ] Bug fix
- [ ] Nuova funzionalità
- [ ] Refactoring (nessun cambio funzionale)
- [ ] CI/CD / Build
- [ ] Documentazione

## Componenti modificati

- [ ] Core (modelli/servizi)
- [ ] CLI
- [ ] UI MAUI
- [ ] Test
- [ ] CI/CD

## Checklist

- [ ] Il codice compila senza errori (`dotnet build eris/eris.sln`)
- [ ] I test passano (`dotnet test eris/Core.Tests/Core.Tests.csproj`)
- [ ] Ho aggiunto test per la nuova funzionalità (se applicabile)
- [ ] Le classi nuove sono `sealed` (se non pensate per ereditarietà)
- [ ] I metodi I/O sono async (niente `.Result` o `.Wait()`)
- [ ] Le stringhe UI sono in `AppStrings.resx`
- [ ] Nessun segreto hardcoded nel codice

## Screenshot (se UI)

<!-- Aggiungi screenshot per modifiche alla UI -->

## Note

<!-- Informazioni aggiuntive per i reviewer -->
