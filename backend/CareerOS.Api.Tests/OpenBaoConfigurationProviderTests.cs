using CareerOS.Api.Infrastructure.OpenBao;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CareerOS.Api.Tests;

public class OpenBaoConfigurationProviderTests
{
    [Fact]
    public void Load_WhenOpenBaoUnreachable_DoesNotThrowAndMaintainsFallback()
    {
        var options = new OpenBaoOptions
        {
            Enabled = true,
            Address = "http://127.0.0.1:59999",
            RoleId = "invalid-role-id",
            SecretId = "invalid-secret-id"
        };

        var provider = new OpenBaoConfigurationProvider(options);

        var exception = Record.Exception(() => provider.Load());
        Assert.Null(exception);

        Assert.False(provider.TryGet("ConnectionStrings:CareerDatabase", out _));
    }

    [Fact]
    public void Load_WhenMissingCredentials_DoesNotThrow()
    {
        var options = new OpenBaoOptions
        {
            Enabled = true,
            Address = "http://localhost:8200",
            RoleId = null,
            SecretId = null
        };

        var provider = new OpenBaoConfigurationProvider(options);

        var exception = Record.Exception(() => provider.Load());
        Assert.Null(exception);
    }

    [Fact]
    public void OpenBaoConfigurationSource_BuildsProvider()
    {
        var options = new OpenBaoOptions
        {
            Address = "http://localhost:8200"
        };
        var source = new OpenBaoConfigurationSource(options);
        var builder = new ConfigurationBuilder();

        var provider = source.Build(builder);

        Assert.NotNull(provider);
        Assert.IsType<OpenBaoConfigurationProvider>(provider);
    }
}
