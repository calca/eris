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
}
