using eris.Core.Models;

namespace Core.Tests;

public class CalendarEventParsingTests
{
    [Fact]
    public void ParseStructuredSubject_WithThreeParts_ExtractsClientProjectTopic()
    {
        var evt = new CalendarEvent { Subject = "Acme | Platform | Code Review" };

        CalendarEvent.ParseStructuredSubject(evt);

        Assert.Equal("Acme", evt.Client);
        Assert.Equal("Platform", evt.Project);
        Assert.Equal("Code Review", evt.Topic);
    }

    [Fact]
    public void ParseStructuredSubject_WithThreePartsWithoutSpaces_ExtractsClientProjectTopic()
    {
        var evt = new CalendarEvent { Subject = "Acme|Platform|Code Review" };

        CalendarEvent.ParseStructuredSubject(evt);

        Assert.Equal("Acme", evt.Client);
        Assert.Equal("Platform", evt.Project);
        Assert.Equal("Code Review", evt.Topic);
    }

    [Fact]
    public void ParseStructuredSubject_WithTwoParts_ExtractsClientAndTopicOnly()
    {
        var evt = new CalendarEvent { Subject = "Acme | Daily" };

        CalendarEvent.ParseStructuredSubject(evt);

        Assert.Equal("Acme", evt.Client);
        Assert.Null(evt.Project);
        Assert.Equal("Daily", evt.Topic);
    }

    [Fact]
    public void ParseStructuredSubject_WithMoreThanThreeParts_JoinsTopicTail()
    {
        var evt = new CalendarEvent { Subject = "Acme | Platform | Sprint | Planning" };

        CalendarEvent.ParseStructuredSubject(evt);

        Assert.Equal("Acme", evt.Client);
        Assert.Equal("Platform", evt.Project);
        Assert.Equal("Sprint | Planning", evt.Topic);
    }

    [Fact]
    public void ParseStructuredSubject_WithoutPipe_DoesNotPopulateFields()
    {
        var evt = new CalendarEvent { Subject = "Meeting senza formato" };

        CalendarEvent.ParseStructuredSubject(evt);

        Assert.Null(evt.Client);
        Assert.Null(evt.Project);
        Assert.Null(evt.Topic);
    }

    // ── Multi-template tests ──────────────────────────────────────────────

    [Fact]
    public void ParseStructuredSubject_MultipleTemplates_UsesFirstMatch()
    {
        var evt = new CalendarEvent { Subject = "Acme | Platform | Code Review" };
        var templates = new[]
        {
            "{Cliente} | {Progetto} | {Topic}",
            "{Progetto} - {Topic}"
        };

        CalendarEvent.ParseStructuredSubject(evt, templates);

        Assert.Equal("Acme", evt.Client);
        Assert.Equal("Platform", evt.Project);
        Assert.Equal("Code Review", evt.Topic);
    }

    [Fact]
    public void ParseStructuredSubject_MultipleTemplates_FallsToSecondWhenFirstDoesNotMatch()
    {
        var evt = new CalendarEvent { Subject = "WebApp - Daily Standup" };
        var templates = new[]
        {
            "{Cliente} | {Progetto} | {Topic}",
            "{Progetto} - {Topic}"
        };

        CalendarEvent.ParseStructuredSubject(evt, templates);

        Assert.Null(evt.Client);
        Assert.Equal("WebApp", evt.Project);
        Assert.Equal("Daily Standup", evt.Topic);
    }

    [Fact]
    public void ParseStructuredSubject_MultipleTemplates_NoMatch_LeavesFieldsNull()
    {
        var evt = new CalendarEvent { Subject = "Simple Meeting" };
        var templates = new[]
        {
            "{Cliente} | {Progetto} | {Topic}",
            "{Progetto} - {Topic}"
        };

        CalendarEvent.ParseStructuredSubject(evt, templates);

        Assert.Null(evt.Client);
        Assert.Null(evt.Project);
        Assert.Null(evt.Topic);
    }

    [Fact]
    public void ParseStructuredSubject_MultipleTemplates_DifferentSeparators()
    {
        var templates = new[]
        {
            "{Cliente} | {Progetto} | {Topic}",
            "{Progetto} / {Topic}",
            "{Progetto} - {Topic}"
        };

        var evtPipe = new CalendarEvent { Subject = "Acme | Platform | Review" };
        CalendarEvent.ParseStructuredSubject(evtPipe, templates);
        Assert.Equal("Acme", evtPipe.Client);
        Assert.Equal("Platform", evtPipe.Project);
        Assert.Equal("Review", evtPipe.Topic);

        var evtSlash = new CalendarEvent { Subject = "Platform / Review" };
        CalendarEvent.ParseStructuredSubject(evtSlash, templates);
        Assert.Equal("Platform", evtSlash.Project);
        Assert.Equal("Review", evtSlash.Topic);

        var evtDash = new CalendarEvent { Subject = "Platform - Review" };
        CalendarEvent.ParseStructuredSubject(evtDash, templates);
        Assert.Equal("Platform", evtDash.Project);
        Assert.Equal("Review", evtDash.Topic);
    }

    [Fact]
    public void ParseStructuredSubject_EmptyTemplateList_UsesDefault()
    {
        var evt = new CalendarEvent { Subject = "Acme | Platform | Code Review" };

        CalendarEvent.ParseStructuredSubject(evt, (IReadOnlyList<string>?)null);

        Assert.Equal("Acme", evt.Client);
        Assert.Equal("Platform", evt.Project);
        Assert.Equal("Code Review", evt.Topic);
    }
}
