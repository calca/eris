using eris.Core.Models;

namespace Core.Tests;

public sealed class SubjectMappingCollectionTests
{
    [Fact]
    public void SetForSourceKey_WithValidEntries_PersistsEntries()
    {
        var collection = new SubjectMappingCollection();
        var entries = new[]
        {
            new SubjectMappingEntry { Subject = "Daily", Include = false, Tag = "Internal" },
            new SubjectMappingEntry { Subject = "Client Meeting", Include = true, Tag = "Billable" },
        };

        collection.SetForSourceKey("source-hash", entries);
        var stored = collection.GetForSourceKey("source-hash");

        Assert.Equal(2, stored.Count);
        Assert.Equal("Daily", stored[0].Subject);
        Assert.False(stored[0].Include);
        Assert.Equal("Internal", stored[0].Tag);
    }

    [Fact]
    public void GetForSourceKey_MissingKey_ReturnsEmptyList()
    {
        var collection = new SubjectMappingCollection();

        var stored = collection.GetForSourceKey("missing");

        Assert.Empty(stored);
    }
}
