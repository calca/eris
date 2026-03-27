---
applyTo: "eris/UI/Views/**"
---

# Istruzioni per UI Views (XAML)

Quando lavori sulle Views MAUI:

## Regole XAML

- **Data binding** per tutto: `{Binding PropertyName}`
- **Zero logica nel code-behind** — solo inizializzazione e binding context
- Supporta tema **dark** — usa colori da `Resources/Styles/`
- Layout per desktop **600×800 px** fisso
- Due tab: **Home** (report) e **Settings** (configurazione)

## Pattern di binding

```xml
<!-- Proprietà semplice -->
<Label Text="{Binding Username}" />

<!-- Comando -->
<Button Text="Genera" Command="{Binding GenerateReportCommand}" />

<!-- Converter -->
<BoxView Color="{Binding IsAuthenticated, Converter={StaticResource BoolToColorConverter}}" />

<!-- Visibilità condizionale -->
<Label IsVisible="{Binding HasError}" Text="{Binding ErrorMessage}" />
```

## Localizzazione

Usa sempre risorse per stringhe visibili:

```xml
<Label Text="{x:Static strings:AppStrings.GenerateButton}" />
```

## Code-behind minimo

```csharp
public partial class MainPage : ContentPage
{
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
```
