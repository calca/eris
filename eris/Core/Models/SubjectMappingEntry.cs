namespace eris.Core.Models;

/// <summary>
/// Regola di mapping manuale per subject.
/// </summary>
public sealed record SubjectMappingEntry
{
    /// <summary>Subject da mappare (normalmente confrontato case-insensitive).</summary>
    public string Subject { get; init; } = string.Empty;

    /// <summary>Se true, l'evento viene incluso; se false, escluso.</summary>
    public bool Include { get; init; } = true;

    /// <summary>Tag opzionale da assegnare all'evento.</summary>
    public string? Tag { get; init; }
}
