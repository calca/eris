using System.Security.Cryptography;
using System.Text;
using eris.Core.Models;

namespace eris.Core.Services;

/// <summary>
/// Utility per costruire una source key hash stabile e deterministica.
/// Input: tipo sorgente + identificatore sorgente (URL ICS o account Graph).
/// </summary>
public static class SourceKeyHasher
{
    public static string Compute(ReportSourceType sourceType, string sourceIdentifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceIdentifier);

        var normalizedType = sourceType.ToString().ToLowerInvariant();
        var normalizedIdentifier = NormalizeIdentifier(sourceType, sourceIdentifier);
        var material = $"{normalizedType}|{normalizedIdentifier}";

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static string NormalizeIdentifier(ReportSourceType sourceType, string sourceIdentifier)
    {
        var trimmed = sourceIdentifier.Trim();

        if (sourceType == ReportSourceType.Graph)
            return trimmed.ToLowerInvariant();

        if (sourceType != ReportSourceType.Ics)
            return trimmed;

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            return trimmed;

        var builder = new UriBuilder(uri)
        {
            Scheme = uri.Scheme.ToLowerInvariant(),
            Host = uri.Host.ToLowerInvariant(),
            Fragment = string.Empty,
        };

        if (builder.Uri.IsDefaultPort)
            builder.Port = -1;

        var canonical = builder.Uri.AbsoluteUri.TrimEnd('/');
        return canonical;
    }
}
