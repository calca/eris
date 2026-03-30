using eris.Core.Models;
using eris.Core.Services;

namespace Core.Tests;

public sealed class SourceKeyHasherTests
{
    [Fact]
    public void Compute_SameInput_ReturnsDeterministicHash()
    {
        var first = SourceKeyHasher.Compute(ReportSourceType.Graph, "user@example.com");
        var second = SourceKeyHasher.Compute(ReportSourceType.Graph, "user@example.com");

        Assert.Equal(first, second);
        Assert.Equal(64, first.Length);
    }

    [Fact]
    public void Compute_GraphSource_IgnoresIdentifierCase()
    {
        var first = SourceKeyHasher.Compute(ReportSourceType.Graph, "User@Example.com");
        var second = SourceKeyHasher.Compute(ReportSourceType.Graph, "user@example.com");

        Assert.Equal(first, second);
    }

    [Fact]
    public void Compute_IcsSource_NormalizesUrlCaseAndTrailingSlash()
    {
        var first = SourceKeyHasher.Compute(ReportSourceType.Ics, "HTTPS://EXAMPLE.COM/calendar.ics/");
        var second = SourceKeyHasher.Compute(ReportSourceType.Ics, "https://example.com/calendar.ics");

        Assert.Equal(first, second);
    }

    [Fact]
    public void Compute_DifferentSourceType_ProducesDifferentHash()
    {
        var graph = SourceKeyHasher.Compute(ReportSourceType.Graph, "user@example.com");
        var ics = SourceKeyHasher.Compute(ReportSourceType.Ics, "user@example.com");

        Assert.NotEqual(graph, ics);
    }
}
