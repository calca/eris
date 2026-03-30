using eris.Core.Models;
using eris.Core.Services;

namespace Core.Tests;

public sealed class ReportOrchestratorFilterTests
{
    [Fact]
    public async Task GenerateAsync_FromExtractedEvents_AppliesSubjectMappingsAndTag()
    {
        var source = new FakeCalendarSource([]);
        var orchestrator = new ReportOrchestrator(source);

        var extractedEvents = new List<CalendarEvent>
        {
            new() { Subject = "Daily", DurationHours = 0.5 },
            new() { Subject = "Client Call", DurationHours = 1.5 },
        };

        var mappings = new List<SubjectMappingEntry>
        {
            new() { Subject = "Daily", Include = false, Tag = "Internal" },
            new() { Subject = "Client Call", Include = true, Tag = "Billable" },
        };

        var range = WeekRange.FromCustom(new DateTime(2026, 3, 23), new DateTime(2026, 3, 27));
        var outputDir = Path.Combine(Path.GetTempPath(), $"eris-tests-{Guid.NewGuid():N}");

        try
        {
            var result = await orchestrator.GenerateAsync(
                extractedEvents,
                range,
                outputDir,
                ExportFormat.Csv,
                subjectMappings: mappings);

            Assert.Equal(1, result.EventCount);
            Assert.Equal(1.5, result.TotalHours, 3);

            var detailContent = await File.ReadAllTextAsync(result.DetailPath);
            var summaryContent = await File.ReadAllTextAsync(result.SummaryPath);

            Assert.Contains("Tag", detailContent);
            Assert.Contains("Tag", summaryContent);
            Assert.Contains("Billable", detailContent);
            Assert.Contains("Billable", summaryContent);
            Assert.DoesNotContain("Daily", detailContent);
        }
        finally
        {
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public async Task GenerateAsync_WithSourceKeyMappings_AppliesEntriesFromCollection()
    {
        var source = new FakeCalendarSource(new List<CalendarEvent>
        {
            new() { Subject = "Ops", DurationHours = 0.5 },
            new() { Subject = "Planning", DurationHours = 1.0 },
        });

        var mappings = new SubjectMappingCollection();
        mappings.SetForSourceKey("source-hash", new[]
        {
            new SubjectMappingEntry { Subject = "Ops", Include = false, Tag = "Internal" },
            new SubjectMappingEntry { Subject = "Planning", Include = true, Tag = "Delivery" },
        });

        var orchestrator = new ReportOrchestrator(source);
        var range = WeekRange.FromCustom(new DateTime(2026, 3, 23), new DateTime(2026, 3, 27));
        var outputDir = Path.Combine(Path.GetTempPath(), $"eris-tests-{Guid.NewGuid():N}");

        try
        {
            var result = await orchestrator.GenerateAsync(
                range,
                outputDir,
                ExportFormat.Csv,
                subjectMappings: mappings,
                sourceKey: "source-hash");

            Assert.Equal(1, result.EventCount);
            Assert.Equal(1.0, result.TotalHours, 3);

            var detailContent = await File.ReadAllTextAsync(result.DetailPath);
            Assert.Contains("Delivery", detailContent);
            Assert.DoesNotContain("Ops", detailContent);
        }
        finally
        {
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public async Task GenerateAsync_WithTopicFilter_ExcludesMatchingEventsFromCount()
    {
        var source = new FakeCalendarSource(new List<CalendarEvent>
        {
            new() { Subject = "A", Topic = "Code Review", DurationHours = 1.0 },
            new() { Subject = "B", Topic = "Daily Team", DurationHours = 0.5 },
            new() { Subject = "C", Topic = "Planning", DurationHours = 1.5 },
        });

        var orchestrator = new ReportOrchestrator(source);
        var range = WeekRange.FromCustom(new DateTime(2026, 3, 23), new DateTime(2026, 3, 27));
        var outputDir = Path.Combine(Path.GetTempPath(), $"eris-tests-{Guid.NewGuid():N}");

        try
        {
            var result = await orchestrator.GenerateAsync(
                range,
                outputDir,
                ExportFormat.Csv,
                new EventFilters { Topics = ["Daily"] });

            Assert.Equal(2, result.EventCount);
            Assert.Equal(2.5, result.TotalHours, 3);
        }
        finally
        {
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public async Task GenerateAsync_WithTopicFilter_IsCaseInsensitive()
    {
        var source = new FakeCalendarSource(new List<CalendarEvent>
        {
            new() { Subject = "A", Topic = "Code Review", DurationHours = 1.0 },
            new() { Subject = "B", Topic = "Daily Team", DurationHours = 0.5 },
        });

        var orchestrator = new ReportOrchestrator(source);
        var range = WeekRange.FromCustom(new DateTime(2026, 3, 23), new DateTime(2026, 3, 27));
        var outputDir = Path.Combine(Path.GetTempPath(), $"eris-tests-{Guid.NewGuid():N}");

        try
        {
            var result = await orchestrator.GenerateAsync(
                range,
                outputDir,
                ExportFormat.Csv,
                new EventFilters { Topics = ["daily"] });

            Assert.Equal(1, result.EventCount);
            Assert.Equal(1.0, result.TotalHours, 3);
        }
        finally
        {
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public async Task GenerateAsync_WithTopicFilter_FallsBackToSubjectWhenTopicIsNull()
    {
        // Events with unstructured subjects have Topic = null
        var source = new FakeCalendarSource(new List<CalendarEvent>
        {
            new() { Subject = "Daily Team", Topic = null, DurationHours = 0.5 },
            new() { Subject = "Code Review", Topic = null, DurationHours = 1.0 },
            new() { Subject = "Acme | Portal | Planning", Topic = "Planning", Client = "Acme", Project = "Portal", DurationHours = 1.5 },
        });

        var orchestrator = new ReportOrchestrator(source);
        var range = WeekRange.FromCustom(new DateTime(2026, 3, 23), new DateTime(2026, 3, 27));
        var outputDir = Path.Combine(Path.GetTempPath(), $"eris-tests-{Guid.NewGuid():N}");

        try
        {
            var result = await orchestrator.GenerateAsync(
                range,
                outputDir,
                ExportFormat.Csv,
                new EventFilters { Topics = ["Daily"] });

            // "Daily Team" should be excluded via Subject fallback
            Assert.Equal(2, result.EventCount);
            Assert.Equal(2.5, result.TotalHours, 3);
        }
        finally
        {
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, recursive: true);
        }
    }

    private sealed class FakeCalendarSource(List<CalendarEvent> events) : ICalendarSource
    {
        public Task<List<CalendarEvent>> GetEventsAsync(WeekRange week) => Task.FromResult(events);
    }
}
