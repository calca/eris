namespace eris.Core.Models;

using System.Text.RegularExpressions;

/// <summary>
/// Rappresenta un evento di calendario estratto da Microsoft Graph o da un file ICS.
/// </summary>
public class CalendarEvent
{
    public string Subject      { get; set; } = string.Empty;
    /// <summary>Prima categoria assegnata all'evento (può essere null).</summary>
    public string? Category    { get; set; }
    /// <summary>Durata in ore (calcolata da start/end UTC restituiti da Graph o DTSTART/DTEND ICS).</summary>
    public double DurationHours { get; set; }

    /// <summary>Data e ora di inizio (locale).</summary>
    public DateTime? StartTime { get; set; }

    /// <summary>Data e ora di fine (locale).</summary>
    public DateTime? EndTime { get; set; }

    /// <summary>Cliente estratto dal subject strutturato "CLIENT | PROJECT | TOPIC".</summary>
    public string? Client  { get; set; }
    /// <summary>Progetto estratto dal subject strutturato.</summary>
    public string? Project { get; set; }
    /// <summary>Attività estratta dal subject strutturato.</summary>
    public string? Topic   { get; set; }

    /// <summary>True se l'evento è in stato tentative (non ancora accettato).</summary>
    public bool IsTentative { get; set; }

    /// <summary>
    /// Parsa il Subject strutturato nei campi Client, Project, Topic
    /// usando il template di default "{Cliente} | {Progetto} | {Topic}".
    /// </summary>
    public static void ParseStructuredSubject(CalendarEvent evt)
        => ParseStructuredSubject(evt, (IReadOnlyList<string>?)null);

    /// <summary>
    /// Parsa il Subject secondo un singolo template configurabile.
    /// </summary>
    public static void ParseStructuredSubject(CalendarEvent evt, string? template)
        => ParseStructuredSubject(evt, template is null ? null : [template]);

    /// <summary>
    /// Parsa il Subject provando più template in ordine.
    /// Il primo template che produce un match (≥ 2 parti) viene utilizzato.
    /// Il template usa placeholder tra graffe: {Cliente}, {Progetto}, {Topic}.
    /// Il separatore è dedotto automaticamente dal testo tra i placeholder.
    /// Esempi di template validi:
    ///   "{Cliente} | {Progetto} | {Topic}"  (default)
    ///   "{Cliente} - {Topic}"
    ///   "{Progetto} / {Cliente} / {Topic}"
    /// </summary>
    public static void ParseStructuredSubject(CalendarEvent evt, IReadOnlyList<string>? templates)
    {
        if (string.IsNullOrWhiteSpace(evt.Subject)) return;

        templates ??= [DefaultTemplate];

        foreach (var template in templates)
        {
            if (!TryApplyTemplate(evt, template))
                continue;
            return; // first match wins
        }
    }

    private static bool TryApplyTemplate(CalendarEvent evt, string template)
    {
        var (separator, fieldNames) = ParseTemplate(template);

        var parts = evt.Subject
            .Split(separator, StringSplitOptions.TrimEntries)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToArray();

        if (parts.Length < 2) return false;

        var assignment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (parts.Length >= fieldNames.Length)
        {
            for (int i = 0; i < fieldNames.Length - 1; i++)
                assignment[fieldNames[i]] = parts[i];
            assignment[fieldNames[^1]] = string.Join($" {separator} ", parts.Skip(fieldNames.Length - 1));
        }
        else // parts.Length < fieldNames.Length ma >= 2
        {
            assignment[fieldNames[0]] = parts[0];
            assignment[fieldNames[^1]] = parts[^1];
            for (int i = 1; i < parts.Length - 1 && i < fieldNames.Length - 1; i++)
                assignment[fieldNames[i]] = parts[i];
        }

        if (assignment.TryGetValue("Cliente", out var c)) evt.Client = c;
        if (assignment.TryGetValue("Progetto", out var p)) evt.Project = p;
        if (assignment.TryGetValue("Topic", out var t)) evt.Topic = t;

        return true;
    }

    public const string DefaultTemplate = "{Cliente} | {Progetto} | {Topic}";

    private static (string separator, string[] fields) ParseTemplate(string? template)
    {
        template ??= DefaultTemplate;

        var matches = Regex.Matches(template, @"\{(\w+)\}");
        var fields = matches.Select(m => m.Groups[1].Value).ToArray();
        if (fields.Length == 0)
            return ("|", ["Cliente", "Progetto", "Topic"]);

        string separator = "|";
        if (matches.Count >= 2)
        {
            var between = template[(matches[0].Index + matches[0].Length)..matches[1].Index].Trim();
            if (!string.IsNullOrEmpty(between))
                separator = between;
        }

        return (separator, fields);
    }
}
