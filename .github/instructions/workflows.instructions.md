---
applyTo: ".github/workflows/**"
---

# Istruzioni per GitHub Actions Workflows

Quando lavori sui workflow CI/CD di eris:

## Setup .NET 10

```yaml
- name: Setup .NET 10
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: '10.0.x'
```

## Piattaforme

### macOS (Mac Catalyst)
```yaml
runs-on: macos-latest
steps:
  - name: Select Xcode 26.2
    run: sudo xcode-select --switch /Applications/Xcode_26.2.app
  - name: Install workload
    run: dotnet workload install maui-maccatalyst
  - name: Build
    run: dotnet publish eris/UI/UI.csproj -f net10.0-maccatalyst -c Release
```

### Windows
```yaml
runs-on: windows-latest
steps:
  - name: Install workloads
    run: dotnet workload install maui-windows maui-maccatalyst
  - name: Build
    run: dotnet publish eris/UI/UI.csproj -f net10.0-windows10.0.19041.0 -c Release
```

## Regole

- Usa sempre `actions/checkout@v4`, `actions/setup-dotnet@v4`, `actions/upload-artifact@v4`
- Permessi: `contents: write` per creare releases
- Artefatti: nomi consistenti (`eris-macos`, `eris-windows`)
- Non hardcodare segreti — usa `${{ secrets.SECRET_NAME }}`
- Test step prima di publish in workflow CI
