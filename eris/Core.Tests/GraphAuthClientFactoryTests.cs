using eris.Core.Models;
using eris.Core.Services;

namespace Core.Tests;

public sealed class GraphAuthClientFactoryTests
{
    private readonly GraphAuthClientFactory _factory = new();

    [Fact]
    public void Create_WithValidClientIdAndDefaultTenant_ReturnsPublicClientApplication()
    {
        var config = new AppConfig
        {
            ClientId = "11111111-1111-1111-1111-111111111111",
            TenantId = "common",
        };

        var app = _factory.Create(config);

        Assert.NotNull(app);
    }

    [Fact]
    public void Create_WithMissingClientId_ThrowsInvalidOperationException()
    {
        var config = new AppConfig
        {
            ClientId = "",
            TenantId = "common",
        };

        var action = () => _factory.Create(config);

        var ex = Assert.Throws<InvalidOperationException>(action);
        Assert.Contains("AzureAd:ClientId is required", ex.Message);
    }

    [Fact]
    public void Create_WithKnownSharedPublicClientId_ThrowsInvalidOperationException()
    {
        var config = new AppConfig
        {
            ClientId = "14d82eec-204b-4c2f-b7e8-296a70dab67e",
            TenantId = "common",
        };

        var action = () => _factory.Create(config);

        var ex = Assert.Throws<InvalidOperationException>(action);
        Assert.Contains("shared public client id", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_WithSpecificTenantIdentifier_ReturnsPublicClientApplication()
    {
        var config = new AppConfig
        {
            ClientId = "11111111-1111-1111-1111-111111111111",
            TenantId = "contoso.onmicrosoft.com",
        };

        var app = _factory.Create(config);

        Assert.NotNull(app);
    }

    [Fact]
    public void Create_WithEmptyTenant_UsesDefaultTenantAndReturnsPublicClientApplication()
    {
        var config = new AppConfig
        {
            ClientId = "11111111-1111-1111-1111-111111111111",
            TenantId = " ",
        };

        var app = _factory.Create(config);

        Assert.NotNull(app);
    }

    [Fact]
    public void Create_WithInvalidClientIdGuid_ThrowsInvalidOperationException()
    {
        var config = new AppConfig
        {
            ClientId = "not-a-guid",
            TenantId = "common",
        };

        var action = () => _factory.Create(config);

        var ex = Assert.Throws<InvalidOperationException>(action);
        Assert.Contains("valid GUID", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_WithInvalidTenantIdentifier_ThrowsInvalidOperationException()
    {
        var config = new AppConfig
        {
            ClientId = "11111111-1111-1111-1111-111111111111",
            TenantId = "https://login.microsoftonline.com/common",
        };

        var action = () => _factory.Create(config);

        var ex = Assert.Throws<InvalidOperationException>(action);
        Assert.Contains("TenantId", ex.Message);
    }
}
