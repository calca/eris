using Microsoft.Identity.Client;

namespace OutlookWeeklyReport.Core.Services;

/// <summary>
/// Gestisce l'autenticazione MSAL per Microsoft Graph.
/// L'istanza di IPublicClientApplication viene iniettata dal layer UI
/// (che opera nel TFM platform-specific e può configurare ASWebAuthenticationSession, WAM, ecc.).
/// </summary>
public sealed class GraphAuthService
{
    private readonly IPublicClientApplication _app;
    private readonly string[] _scopes;

    public GraphAuthService(IPublicClientApplication app, string[] scopes)
    {
        _app = app;
        _scopes = scopes;

        TokenCacheHelper.EnableSerialization(_app.UserTokenCache);
    }

    /// <summary>
    /// Restituisce un access token valido.
    /// Tenta prima il cache silenzioso, poi il flusso interattivo.
    /// </summary>
    /// <param name="parentWindow">
    /// Su iOS/Mac Catalyst: il UIViewController corrente.
    /// Su Windows: l'HWND della finestra. Null per CLI (usa system browser).
    /// </param>
    public async Task<string> GetAccessTokenAsync(object? parentWindow = null)
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

        // 2. Flusso interattivo
        var request = _app
            .AcquireTokenInteractive(_scopes)
            .WithPrompt(Prompt.SelectAccount);

        if (parentWindow != null)
            request.WithParentActivityOrWindow(parentWindow);

        var interactive = await request.ExecuteAsync();
        return interactive.AccessToken;
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
