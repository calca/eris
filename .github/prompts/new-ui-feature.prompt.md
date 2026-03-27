---
description: 'Aggiungi una nuova feature alla UI MAUI (view + viewmodel + bindings)'
---

# Nuova Feature UI MAUI

Implementa una nuova funzionalità nella UI MAUI di eris.

## Architettura UI

- **MVVM Toolkit**: usa `[ObservableProperty]`, `[RelayCommand]`, `[NotifyPropertyChangedFor]`, `[NotifyCanExecuteChangedFor]`
- **ViewModel**: `eris/UI/ViewModels/MainViewModel.cs` — viewmodel principale, aggiungi proprietà/comandi qui
- **Views**: XAML in `eris/UI/Views/` con code-behind minimo
- **Converters**: `eris/UI/Converters/` — suffisso `Converter`
- **Risorse stringa**: `eris/UI/Resources/Strings/AppStrings.resx` — testo in italiano

## Convenzioni

1. **Data binding**: usa `{Binding PropertyName}` in XAML, mai code-behind per logica
2. **Comandi asincroni**: `[RelayCommand]` su metodi `async Task`
3. **Stile**: tema dark, colori definiti in `Resources/Styles/`
4. **Layout**: desktop-only 600×800 px, due tab (Home + Settings)
5. **Piattaforma**: Mac Catalyst + Windows — evita API platform-specific dove possibile
6. **Localizzazione**: ogni stringa visibile deve essere in `AppStrings.resx`

## Feature da implementare

Descrizione: {{description}}
Tab di destinazione: {{tab}}
