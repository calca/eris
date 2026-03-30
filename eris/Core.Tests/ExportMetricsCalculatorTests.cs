using eris.Core.ExportServices;
using eris.Core.Models;

namespace Core.Tests;

public sealed class ExportMetricsCalculatorTests
{
    [Fact]
    public void Compute_WithMixedEvents_AggregatesSummaryRowsByBusinessKeys()
    {
        // Arrange
        var calculator = new ExportMetricsCalculator();
        var events = new List<CalendarEvent>
        {
            new() { Subject = "S1", Category = "CatA", Client = "ClientA", Project = "ProjA", Topic = "Topic1", Tag = "Billable", DurationHours = 2.0 },
            new() { Subject = "S2", Category = "CatA", Client = "ClientA", Project = "ProjA", Topic = "Topic2", Tag = "Billable", DurationHours = 1.0 },
            new() { Subject = "S3", Category = null, Client = "ClientB", Project = "ProjB", Topic = "TopicX", Tag = "Billable", DurationHours = 1.0 },
            new() { Subject = "S4", Category = null, Client = "ClientB", Project = "ProjB", Topic = "TopicY", Tag = "Billable", DurationHours = 1.0 },
            new() { Subject = "SubjectOnly", Category = null, Client = "ClientC", Project = null, Topic = "TopicSolo", Tag = null, DurationHours = 0.5 },
        };

        // Act
        var result = calculator.Compute(events, weeklyHours: 10);

        // Assert
        Assert.Equal(3, result.SummaryRows.Count);

        var categoryRow = result.SummaryRows.Single(r => r.Category == "CatA");
        Assert.Equal("ClientA", categoryRow.Client);
        Assert.Equal("ProjA", categoryRow.Project);
        Assert.Equal(string.Empty, categoryRow.Topic);
        Assert.Equal("Billable", categoryRow.Tag);
        Assert.Equal(2, categoryRow.MeetingCount);
        Assert.Equal(3.0, categoryRow.TotalHours, 3);

        var projectRow = result.SummaryRows.Single(r => r.Category == string.Empty && r.Project == "ProjB");
        Assert.Equal(string.Empty, projectRow.Topic);
        Assert.Equal(2, projectRow.MeetingCount);
        Assert.Equal(2.0, projectRow.TotalHours, 3);

        var topicFallbackRow = result.SummaryRows.Single(r => r.Project == string.Empty && r.Topic == "TopicSolo");
        Assert.Equal(1, topicFallbackRow.MeetingCount);
        Assert.Equal(0.5, topicFallbackRow.TotalHours, 3);
    }

    [Fact]
    public void Compute_WithTaggedEvents_BuildsSummaryByTagRows()
    {
        // Arrange
        var calculator = new ExportMetricsCalculator();
        var events = new List<CalendarEvent>
        {
            new() { Subject = "A1", Tag = "A", DurationHours = 1.0 },
            new() { Subject = "A2", Tag = "A", DurationHours = 2.0 },
            new() { Subject = "B1", Tag = "B", DurationHours = 1.0 },
            new() { Subject = "N1", Tag = null, DurationHours = 1.0 },
        };

        // Act
        var result = calculator.Compute(events, weeklyHours: 8);

        // Assert
        Assert.Equal(3, result.SummaryByTagRows.Count);

        var tagA = result.SummaryByTagRows.Single(r => r.Tag == "A");
        Assert.Equal(2, tagA.MeetingCount);
        Assert.Equal(3.0, tagA.TotalHours, 3);

        var tagB = result.SummaryByTagRows.Single(r => r.Tag == "B");
        Assert.Equal(1, tagB.MeetingCount);
        Assert.Equal(1.0, tagB.TotalHours, 3);

        var emptyTag = result.SummaryByTagRows.Single(r => r.Tag == string.Empty);
        Assert.Equal(1, emptyTag.MeetingCount);
        Assert.Equal(1.0, emptyTag.TotalHours, 3);
    }

    [Fact]
    public void Compute_WithWeeklyHoursGreaterThanTotal_ComputesPercentagesAndInternalHours()
    {
        // Arrange
        var calculator = new ExportMetricsCalculator();
        var events = new List<CalendarEvent>
        {
            new() { Subject = "A", Tag = "TagA", DurationHours = 3.0 },
            new() { Subject = "B", Tag = "TagB", DurationHours = 2.0 },
            new() { Subject = "C", Tag = "TagC", DurationHours = 0.5 },
        };

        // Act
        var result = calculator.Compute(events, weeklyHours: 10);

        // Assert
        var rowA = result.SummaryByTagRows.Single(r => r.Tag == "TagA");
        Assert.Equal("30.0%", rowA.ShareOfWeeklyHours);
        Assert.Equal("54.5%", rowA.ShareOfMeetingHours);
        Assert.Equal(2.45, rowA.InternalHours, 2);
        Assert.Equal(5.45, rowA.TotalSpentHours, 2);

        Assert.Equal(3, result.Totals.MeetingCount);
        Assert.Equal(5.5, result.Totals.MeetingHours, 3);
        Assert.Equal("55.0%", result.Totals.ShareOfWeeklyHours);
        Assert.Equal("100.0%", result.Totals.ShareOfMeetingHours);
        Assert.Equal(4.5, result.Totals.InternalHours, 3);
        Assert.Equal(10.0, result.Totals.TotalSpentHours, 3);
    }

    [Fact]
    public void Compute_WithNonPositiveWeeklyHours_UsesMeetingHoursAsPercentBase()
    {
        // Arrange
        var calculator = new ExportMetricsCalculator();
        var events = new List<CalendarEvent>
        {
            new() { Subject = "One", Tag = "X", DurationHours = 2.0 },
        };

        // Act
        var result = calculator.Compute(events, weeklyHours: 0);

        // Assert
        var row = Assert.Single(result.SummaryByTagRows);
        Assert.Equal("100.0%", row.ShareOfWeeklyHours);
        Assert.Equal("100.0%", row.ShareOfMeetingHours);
        Assert.Equal(0, row.InternalHours);
        Assert.Equal(2.0, row.TotalSpentHours, 3);

        Assert.Equal("100.0%", result.Totals.ShareOfWeeklyHours);
        Assert.Equal("100.0%", result.Totals.ShareOfMeetingHours);
        Assert.Equal(0, result.Totals.InternalHours);
    }

    [Fact]
    public void Compute_WithNoEvents_ReturnsEmptyRowsAndZeroMeetingTotals()
    {
        // Arrange
        var calculator = new ExportMetricsCalculator();

        // Act
        var result = calculator.Compute([], weeklyHours: 40);

        // Assert
        Assert.Empty(result.SummaryRows);
        Assert.Empty(result.SummaryByTagRows);
        Assert.Equal(0, result.Totals.MeetingCount);
        Assert.Equal(0, result.Totals.MeetingHours, 3);
        Assert.Equal("0.0%", result.Totals.ShareOfWeeklyHours);
        Assert.Equal("0%", result.Totals.ShareOfMeetingHours);
        Assert.Equal(40, result.Totals.InternalHours, 3);
        Assert.Equal(40, result.Totals.TotalSpentHours, 3);
    }
}
