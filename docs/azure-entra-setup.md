# Azure Entra ID Setup for eris (CLI + UI)

Questa guida configura un'app registration **dedicata** per eris, valida su macOS e Windows, con login Microsoft tramite MSAL per:

- account personali Outlook.com / Live / Hotmail
- account aziendali o scolastici Microsoft 365 (Entra ID)

Non usa scorciatoie: niente app-id pubbliche condivise, niente device code flow.

## 1. Prerequisiti

- Accesso al portale Azure: <https://portal.azure.com>
- Permessi per creare un'App Registration nel tenant (oppure supporto amministratore)
- Repository eris locale

## 2. Crea l'App Registration

1. Vai in Microsoft Entra ID -> App registrations -> New registration.
2. Nome suggerito: `eris-local`.
3. Supported account types:
   - `Accounts in any organizational directory and personal Microsoft accounts`
   - Questa opzione abilita sia M365 aziendale che account personali.
4. Redirect URI (Public client/native):
   - Platform: `Mobile and desktop applications`
   - URI: `http://localhost`
5. Conferma con Register.

## 3. Configura Authentication

Apri l'app registrata -> Authentication:

1. Verifica che esista la piattaforma `Mobile and desktop applications`.
2. Verifica il redirect URI `http://localhost`.
3. Non configurare client secret (eris usa un public client nativo con PKCE).

## 4. Configura API Permissions

Apri API permissions -> Add a permission -> Microsoft Graph -> Delegated permissions e aggiungi:

- `Calendars.Read`
- `User.Read`

Poi:

- Per account personali: il consenso e richiesto all'utente in login.
- Per tenant aziendali con policy restrittive: un admin puo concedere `Grant admin consent`.

## 5. Recupera valori da usare in eris

Dalla pagina Overview dell'app registration:

- `Application (client) ID` -> `AzureAd:ClientId`
- Tenant consigliato per supportare personal + aziendale: `common`
  - in alternativa puoi usare `organizations`, `consumers` o un tenant specifico.

## 6. Configura eris CLI

Modifica `eris/CLI/appsettings.json`:

```json
{
  "AzureAd": {
    "ClientId": "<YOUR-APP-CLIENT-ID>",
    "TenantId": "common",
    "Scopes": [ "Calendars.Read", "User.Read" ]
  }
}
```

Per onboarding rapido puoi partire da `eris/CLI/appsettings.template.json`.

## 6.b Configura eris UI

Se vuoi distribuire una configurazione locale anche per UI, usa `eris/UI/appsettings.template.json`
come base per un `appsettings.json` nel percorso di deployment dell'app.

## 7. Configura eris via environment variables (opzionale)

In alternativa al file JSON:

```bash
export ERIS_AzureAd__ClientId="<YOUR-APP-CLIENT-ID>"
export ERIS_AzureAd__TenantId="common"
```

Nota: il prefisso corretto e `ERIS_`.

## 8. Verifica login CLI

```bash
dotnet run --project eris/CLI/CLI.csproj -- test-login
```

Atteso:

- apertura browser Microsoft login
- selezione account personale o aziendale
- consenso ai permessi Graph
- messaggio `Login riuscito`

## 9. Verifica login UI (Mac/Windows)

1. Avvia UI MAUI.
2. Vai nella tab Settings.
3. Seleziona `Outlook` come sorgente.
4. Clicca `Sign in with Microsoft`.
5. Completa login nel browser.

Atteso: stato autenticato e nome utente mostrato in UI.

## 10. Troubleshooting

- Errore `AzureAd:ClientId is required`:
  - manca ClientId in config o env.
- Errore su `shared public client id`:
  - stai usando un app-id pubblico condiviso, crea una app registration dedicata.
- Errore consenso in tenant aziendale:
  - serve admin consent per Graph delegated permissions.
- Login apre browser ma non torna in app:
  - ricontrolla redirect URI `http://localhost` e piattaforma `Mobile and desktop applications`.

## 11. Security notes

- Non inserire client secret in eris.
- Mantieni scopes minimi (`Calendars.Read`, `User.Read`).
- Il token cache e locale; su Windows viene protetto con DPAPI (utente corrente).
