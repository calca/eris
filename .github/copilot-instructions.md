# Copilot Instructions — eris

## Project Overview

**eris** is a .NET 10 multiplatform calendar meeting report generator. It fetches calendar events (Microsoft Graph or ICS files) and exports weekly/monthly reports to CSV or XLSX with detailed breakdowns and summary statistics.

### Architecture

```
eris.sln
├── Core/          → Shared library (net10.0): models, services, business logic
├── CLI/           → Console app (net10.0): System.CommandLine + Spectre.Console
├── UI/            → MAUI desktop app (net10.0-maccatalyst / net10.0-windows10.0.19041.0)
└── Core.Tests/    → xUnit tests for Core
```

- **Core** is platform-agnostic and shared by both CLI and UI.
- **CLI** is a terminal interface with interactive mode and command-line options.
- **UI** is a MAUI desktop-only app (600×800 px) with two tabs: Home (reports) and Settings.

### Namespaces

- `eris.Core`, `eris.Core.Models`, `eris.Core.Services`
- `eris.CLI`
- `eris.UI`, `eris.UI.ViewModels`, `eris.UI.Views`, `eris.UI.Converters`

## Coding Conventions

### Language & Framework

- **C# 13+ / .NET 10** — use the latest stable language features.
- `#nullable enable` is active everywhere — always handle nullability properly.
- `ImplicitUsings` is enabled — do not add redundant `using` statements.
- Top-level statements in `Program.cs`.
- File-scoped namespaces (`namespace X;`).
- Use `sealed` on classes that are not intended for inheritance.

### Patterns

- **Interface segregation**: `ICalendarSource`, `IExportService` — keep interfaces focused.
- **MVVM Toolkit** in UI: `[ObservableProperty]`, `[RelayCommand]`, `[NotifyPropertyChangedFor]`, `[NotifyCanExecuteChangedFor]`.
- **DI/IoC**: constructor injection for services in both CLI and UI.
- **Async/Await**: full async pipeline — never block with `.Result` or `.Wait()`.
- **Immutable models**: prefer `record` or `sealed class` with init-only properties for data transfer.

### Naming

- **PascalCase** for public members, types, methods, properties.
- **camelCase** for local variables and private fields (no `_` prefix for fields with `[ObservableProperty]`).
- Suffix services with `Service` (e.g., `CalendarService`, `CsvExportService`).
- Suffix view models with `ViewModel` (e.g., `MainViewModel`).
- Suffix converters with `Converter` (e.g., `BoolToColorConverter`).

### Structured Subject Parsing

Events use structured subjects like `"Client | Project | Topic"`. The parser is in `CalendarEvent.ParseStructuredSubject()` and supports configurable templates.

### Localization

- Italian strings in `AppStrings.resx` — always add new UI text as a resource.
- CSV export uses Italian locale formatting with semicolons as delimiters and UTF-8 BOM.

### Error Handling

- Silent fallbacks for non-critical errors (e.g., token cache corruption).
- Never throw exceptions from UI event handlers — catch and display to user.
- Validate at boundaries (user input, external APIs), trust internal code.

### Testing

- **xUnit** with `FluentAssertions`-style assertions.
- Use `FakeCalendarSource` (implements `ICalendarSource`) for mocking calendar data.
- Test naming: `MethodName_Scenario_ExpectedResult`.
- Always test filtering logic (case-insensitive, partial matching).

## Build & Run

```bash
# Local .NET 10 setup
.vscode/install-dotnet-local.sh

# Build CLI
DOTNET_ROOT="$HOME/.dotnet-local" "$HOME/.dotnet-local/dotnet" build eris/CLI/CLI.csproj

# Build UI (Mac Catalyst)
DOTNET_ROOT="$HOME/.dotnet-local" "$HOME/.dotnet-local/dotnet" build eris/UI/UI.csproj -f net10.0-maccatalyst

# Run tests
dotnet test eris/Core.Tests/Core.Tests.csproj
```

## Key Dependencies

| Package | Used In | Purpose |
|---------|---------|---------|
| Microsoft.Graph 5.x | Core | Calendar API |
| Microsoft.Identity.Client 4.x | Core | MSAL authentication |
| CsvHelper | Core | CSV export |
| ClosedXML | Core | XLSX export |
| Ical.Net | Core | ICS file parsing |
| System.CommandLine | CLI | Command-line parsing |
| Spectre.Console | CLI | Rich terminal UI |
| CommunityToolkit.Mvvm | UI | MVVM source generators |
| CommunityToolkit.Maui | UI | MAUI extensions |

## Platform Notes

- **macOS**: Ad-hoc code signing, interpreter mode (no AOT), Xcode 26.2 for CI.
- **Windows**: Unpackaged executable, minimum Windows 10 build 17763, DPAPI for token encryption.
- **Mac Catalyst MSAL**: May require device code flow fallback — handle gracefully.
