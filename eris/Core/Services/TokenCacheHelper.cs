using Microsoft.Identity.Client;

namespace eris.Core.Services;

/// <summary>
/// Serializzazione del token cache MSAL su disco.
/// Permette il riuso silenzioso del token tra sessioni.
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
                args.TokenCache.DeserializeMsalV3(File.ReadAllBytes(CacheFilePath));
            }
            catch
            {
                // Cache corrotta — la ignoriamo; l'utente dovrà rifare il login
            }
        }
    }

    private static void AfterAccess(TokenCacheNotificationArgs args)
    {
        if (!args.HasStateChanged) return;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CacheFilePath)!);
            File.WriteAllBytes(CacheFilePath, args.TokenCache.SerializeMsalV3());
        }
        catch
        {
            // Errore di scrittura non bloccante
        }
    }
}
