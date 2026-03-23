# 📅 Outlook Weekly Report

Applicazione .NET 8 multipiattaforma (Windows / macOS) per esportare i meeting Outlook accettati in CSV settimanale, con autenticazione Microsoft Graph tramite browser.

---

## 🗂️ Struttura della soluzione

```
OutlookWeeklyReport/
├── OutlookWeeklyReport.sln
├── Core/               — Logica condivisa (modelli, servizi)
├── CLI/                — Interfaccia da terminale (System.CommandLine + Spectre.Console)
└── UI/                 — App MAUI dark-theme (Windows 10/11 + macOS)
```

---

## 🚀 Avvio rapido

### Prerequisiti

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- Per la UI MAUI: `dotnet workload install maui`
- Account Microsoft con Outlook Calendar attivo

### CLI

```bash
cd OutlookWeeklyReport/CLI
dotnet run -- generate --week this
dotnet run -- generate --week last --output ~/Documenti/Report
dotnet run -- whoami
```

### UI MAUI (macOS)

```bash
cd OutlookWeeklyReport/UI
dotnet run -f net8.0-maccatalyst
```

### UI MAUI (Windows)

```powershell
cd OutlookWeeklyReport\UI
dotnet run -f net8.0-windows10.0.19041.0
```

---

## 🔐 Autenticazione

L'app usa il **Client ID pubblico Microsoft** (`04b07795-8542-4c4c-9b00-4c4c9b00c4c4`), lo stesso usato da Azure CLI.  
**Nessuna App Registration richiesta.** Funziona con account personali (Outlook.com) e aziendali (M365).

### Flusso

1. Prima apertura → si apre il browser con la pagina di login Microsoft
2. Accetta i permessi `Calendars.Read` e `User.Read`
3. Il token viene salvato localmente (MSAL token cache) — i login successivi sono silenziosi
4. Fallback headless (SSH/WSL): viene mostrato un codice dispositivo da usare su qualsiasi browser

### Override configurazione

```json
// CLI/appsettings.json  oppure  UI/appsettings.json
{
  "AzureAd": {
    "ClientId": "IL-TUO-CLIENT-ID",
    "TenantId": "IL-TUO-TENANT-ID"
  }
}
```

Oppure tramite variabili d'ambiente:

```bash
export OWREPORT_AzureAd__ClientId="xxxx"
export OWREPORT_AzureAd__TenantId="yyyy"
```

---

## 📁 Output generato

```
<cartella scelta>/
└── week-12-2026-report/
    ├── detail.csv     — Elenco dettagliato eventi (categoria; nome; ore)
    └── summary.csv    — Riepilogo per categoria con % sul totale
```

Entrambi i file usano `;` come separatore e UTF-8 con BOM (compatibili con Excel italiano).

---

## 📦 Dipendenze principali

| Progetto | Pacchetto | Versione |
|----------|-----------|----------|
| Core | `Microsoft.Graph` | 5.56 |
| Core | `Microsoft.Identity.Client` | 4.61 |
| Core | `CsvHelper` | 33.0 |
| CLI  | `System.CommandLine` | 2.0-beta4 |
| CLI  | `Spectre.Console` | 0.49 |
| UI   | `Microsoft.Maui.Controls` | 8.0.91 |
| UI   | `CommunityToolkit.Maui` | 9.0 |
| UI   | `CommunityToolkit.Mvvm` | 8.3 |

---

## ❓ FAQ

**Q: Funziona con account aziendale M365?**  
A: Sì. Se il tenant ha policy restrittive, l'admin IT può fare un *admin consent* oppure registrare una propria App e inserire il ClientId in `appsettings.json`.

**Q: I miei dati vengono inviati a server esterni?**  
A: No. L'app chiama solo `graph.microsoft.com` (Microsoft). I CSV vengono scritti solo in locale.

**Q: Come gestisce gli eventi ricorrenti?**  
A: Usa `calendarView` che espande automaticamente le occorrenze — ogni istanza appare come evento singolo.
