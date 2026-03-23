using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Authentication;
using OutlookWeeklyReport.Core.Models;

namespace OutlookWeeklyReport.Core.Services;

/// <summary>
/// Recupera gli eventi del calendario tramite Microsoft Graph (calendarView).
/// Gestisce la paginazione automatica e filtra solo gli eventi accettati / organizzati dall'utente.
/// </summary>
public sealed class CalendarService
{
    private readonly GraphServiceClient _client;

    public CalendarService(string accessToken)
    {
        var auth    = new BaseBearerTokenAuthenticationProvider(new StaticTokenProvider(accessToken));
        _client = new GraphServiceClient(auth);
    }

    public async Task<List<CalendarEvent>> GetAcceptedEventsAsync(WeekRange week)
    {
        var allEvents = new List<Event>();

        // calendarView espande le ricorrenze — ogni occorrenza appare come evento singolo
        var response = await _client.Me.CalendarView.GetAsync(cfg =>
        {
            cfg.QueryParameters.StartDateTime =
                week.Start.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
            cfg.QueryParameters.EndDateTime =
                week.End.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
            cfg.QueryParameters.Select = ["subject", "start", "end", "responseStatus", "categories", "organizer"];
            cfg.QueryParameters.Top = 100;
        });

        if (response?.Value != null)
            allEvents.AddRange(response.Value);

        // Paginazione manuale su OdataNextLink
        var nextLink = response?.OdataNextLink;
        while (!string.IsNullOrEmpty(nextLink))
        {
            var page = await _client.Me.CalendarView.WithUrl(nextLink).GetAsync();
            if (page?.Value != null)
                allEvents.AddRange(page.Value);
            nextLink = page?.OdataNextLink;
        }

        return allEvents
            .Where(IsAcceptedOrOrganizer)
            .Select(MapToCalendarEvent)
            .ToList();
    }

    private static bool IsAcceptedOrOrganizer(Event e)
    {
        var status = e.ResponseStatus?.Response;
        return status == ResponseType.Accepted || status == ResponseType.Organizer;
    }

    private static CalendarEvent MapToCalendarEvent(Event e)
    {
        var duration = CalculateDurationHours(e.Start, e.End);
        return new CalendarEvent
        {
            Subject       = e.Subject ?? string.Empty,
            Category      = e.Categories?.FirstOrDefault(),
            DurationHours = duration,
        };
    }

    private static double CalculateDurationHours(DateTimeTimeZone? start, DateTimeTimeZone? end)
    {
        if (start?.DateTime == null || end?.DateTime == null) return 0;

        // Graph restituisce dateTime nel fuso dell'utente — la differenza è comunque corretta
        if (DateTime.TryParse(start.DateTime, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var s) &&
            DateTime.TryParse(end.DateTime, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var en))
        {
            return Math.Max(0, (en - s).TotalHours);
        }

        return 0;
    }
}

/// <summary>Fornisce un token Bearer statico per le richieste Graph (pattern Kiota).</summary>
file sealed class StaticTokenProvider : IAccessTokenProvider
{
    private readonly string _token;

    public StaticTokenProvider(string token) => _token = token;

    public AllowedHostsValidator AllowedHostsValidator { get; } = new();

    public Task<string> GetAuthorizationTokenAsync(
        Uri uri,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(_token);
}
