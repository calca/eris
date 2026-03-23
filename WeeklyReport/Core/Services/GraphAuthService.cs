using Microsoft.Identity.Client;
using OutlookWeeklyReport.Core.Models;

namespace OutlookWeeklyReport.Core.Services;

/// <summary>
/// Gestisce l'autenticazione MSAL per Microsoft Graph.
/// Strategia: cache silenzioso → browser interattivo → device code flow (fallback headless).
/// </summary>
public sealed class GraphAuthService
{
    private readonly IPublicClientApplication _app;
    private readonly string[] _scopes;

    public GraphAuthService(AppConfig config)
    {
        _scopes = config.Scopes;

        _app = PublicClientApplicationBuilder
            .Create(config.ClientId)
            .WithAuthority(AzureCloudInstance.AzurePublic, config.TenantId)
            .WithDefaultRedirectUri()
            .Build();

        TokenCacheHelper.EnableSerialization(_app.UserTokenCache);
    }

    /// <summary>
    /// Restituisce un access token valido.
    /// <paramref name="deviceCodeCallback"/> viene invocato se non è disponibile un browser (es. SSH/headless).
    /// </summary>
    public async Task<string> GetAccessTokenAsync(Action<string>? deviceCodeCallback = null)
    {
        // 1. Tentativo silenzioso (token cache)
        var accounts = (await _app.GetAccountsAsync()).ToList();
        if (accounts.Count > 0)
        {
            try
            {
                var silent = await _app.AcquireTokenSilent(_scopes, accounts[0]).ExecuteAsync();
                return silent.AccessToken;
            }
            catch (MsalUiRequiredException) { /* continua con il flusso interattivo */ }
        }

        // 2. Browser interattivo (sistema, non embedded webview)
        try
        {
            var interactive = await _app
                .AcquireTokenInteractive(_scopes)
                .WithPrompt(Prompt.SelectAccount)
                .WithUseEmbeddedWebView(false)
                .ExecuteAsync();
            return interactive.AccessToken;
        }
        catch (OperationCanceledException)
        {
            throw; // L'utente ha annullato esplicitamente
        }
        catch (MsalClientException)
        {
            // Browser non disponibile → device code
        }
        catch (MsalServiceException)
        {
            // Errore di servizio → device code
        }
        catch (Exception)
        {
            // Catch-all → device code
        }

        // 3. Device code flow (headless / SSH)
        var dcr = await _app
            .AcquireTokenWithDeviceCode(_scopes, result =>
            {
                var msg = result.Message;
                deviceCodeCallback?.Invoke(msg);
                if (deviceCodeCallback == null)
                    Console.WriteLine(msg);
                return Task.CompletedTask;
            })
            .ExecuteAsync();

        return dcr.AccessToken;
    }

    /// <summary>Restituisce lo username dell'account autenticato (o stringa vuota).</summary>
    public async Task<string> GetUserDisplayNameAsync()
    {
        var accounts = await _app.GetAccountsAsync();
        return accounts.FirstOrDefault()?.Username ?? string.Empty;
    }

    /// <summary>Rimuove tutti gli account dalla cache locale (logout locale, non revoca il token server).</summary>
    public async Task SignOutAsync()
    {
        foreach (var account in await _app.GetAccountsAsync())
            await _app.RemoveAsync(account);
    }
}
