using Microsoft.Identity.Client;
using eris.Core.Models;

namespace eris.Core.Services;

public interface IGraphAuthClientFactory
{
    IPublicClientApplication Create(AppConfig config);
}

/// <summary>
/// Costruisce un client MSAL coerente per Mac/Windows con authority valida,
/// redirect loopback e validazioni di sicurezza.
/// </summary>
public sealed class GraphAuthClientFactory : IGraphAuthClientFactory
{
    private const string LoopbackRedirectUri = "http://localhost";
    private const string DefaultTenantId = "common";
    private static readonly HashSet<string> ForbiddenPublicClientIds =
    [
        "14d82eec-204b-4c2f-b7e8-296a70dab67e", // Azure CLI public client
    ];

    public IPublicClientApplication Create(AppConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var clientId = ValidateClientId(config.ClientId);
        var tenantId = NormalizeTenantId(config.TenantId);

        var builder = ConfigureAuthority(PublicClientApplicationBuilder.Create(clientId), tenantId)
            .WithRedirectUri(LoopbackRedirectUri);

        return builder.Build();
    }

    internal static PublicClientApplicationBuilder ConfigureAuthority(
        PublicClientApplicationBuilder builder,
        string tenantId)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (tenantId.Equals("common", StringComparison.OrdinalIgnoreCase))
            return builder.WithAuthority(AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount);

        if (tenantId.Equals("organizations", StringComparison.OrdinalIgnoreCase))
            return builder.WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs);

        if (tenantId.Equals("consumers", StringComparison.OrdinalIgnoreCase))
            return builder.WithAuthority(AadAuthorityAudience.PersonalMicrosoftAccount);

        if (tenantId.Contains('/'))
            throw new InvalidOperationException(
                "AzureAd:TenantId must be one of common/organizations/consumers or a specific tenant identifier.");

        return builder.WithAuthority(AzureCloudInstance.AzurePublic, tenantId);
    }

    private static string ValidateClientId(string? clientId)
    {
        var normalized = clientId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException(
                "AzureAd:ClientId is required. Configure a dedicated app registration for this application.");
        }

        if (ForbiddenPublicClientIds.Contains(normalized))
        {
            throw new InvalidOperationException(
                "AzureAd:ClientId cannot use a shared public client id. Configure your own app registration client id.");
        }

        if (!Guid.TryParse(normalized, out _))
            throw new InvalidOperationException("AzureAd:ClientId must be a valid GUID.");

        return normalized;
    }

    private static string NormalizeTenantId(string? tenantId)
    {
        var normalized = tenantId?.Trim() ?? string.Empty;
        return string.IsNullOrWhiteSpace(normalized) ? DefaultTenantId : normalized;
    }
}
