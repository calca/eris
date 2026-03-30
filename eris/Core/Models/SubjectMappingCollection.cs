namespace eris.Core.Models;

/// <summary>
/// Contenitore di mapping manuali subject-based, indicizzati per source key hash.
/// </summary>
public sealed class SubjectMappingCollection
{
    /// <summary>
    /// Dizionario delle regole per sorgente.
    /// Key: source key hash stabile calcolata con SourceKeyHasher.
    /// Value: lista mapping subject -> (include, tag).
    /// </summary>
    public Dictionary<string, List<SubjectMappingEntry>> BySourceKey { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<SubjectMappingEntry> GetForSourceKey(string? sourceKey)
    {
        if (string.IsNullOrWhiteSpace(sourceKey))
            return [];

        return BySourceKey.TryGetValue(sourceKey, out var entries)
            ? entries
            : [];
    }

    public void SetForSourceKey(string sourceKey, IEnumerable<SubjectMappingEntry> entries)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceKey);
        ArgumentNullException.ThrowIfNull(entries);

        BySourceKey[sourceKey] =
            entries.Where(e => !string.IsNullOrWhiteSpace(e.Subject)).ToList();
    }
}
