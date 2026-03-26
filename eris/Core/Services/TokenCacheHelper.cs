using Microsoft.Identity.Client;
using System.Security.Cryptography;

namespace eris.Core.Services;

/// <summary>
/// Serializzazione del token cache MSAL su disco.
/// Permette il riuso silenzioso del token tra sessioni.
/// Su Windows il contenuto viene cifrato con DPAPI (utente corrente) per evitare
/// falsi positivi degli antivirus (es. Windows Defender).
/// </summary>
internal static class TokenCacheHelper
{
    private static readonly string CacheFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "eris",
        "msal_token_cache.bin");

    public static void EnableSerialization(ITokenCache tokenCache)
    {
        tokenCache.SetBeforeAccess(BeforeAccess);
        tokenCache.SetAfterAccess(AfterAccess);
    }

    private static void BeforeAccess(TokenCacheNotificationArgs args)
    {
        if (File.Exists(CacheFilePath))
        {
            try
            {
                var data = File.ReadAllBytes(CacheFilePath);
                if (OperatingSystem.IsWindows())
                    data = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
                args.TokenCache.DeserializeMsalV3(data);
            }
            catch
            {
                // Cache corrotta o non decifrabile — la ignoriamo; l'utente dovrà rifare il login
            }
        }
    }

    private static void AfterAccess(TokenCacheNotificationArgs args)
    {
        if (!args.HasStateChanged) return;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CacheFilePath)!);
            var data = args.TokenCache.SerializeMsalV3();
            if (OperatingSystem.IsWindows())
                data = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(CacheFilePath, data);
        }
        catch
        {
            // Errore di scrittura non bloccante
        }
    }
}
