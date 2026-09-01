using Microsoft.Extensions.Configuration;

namespace CareerOS.Api.Infrastructure.OpenBao;

public class OpenBaoConfigurationSource : IConfigurationSource
{
    public OpenBaoOptions Options { get; }

    public OpenBaoConfigurationSource(OpenBaoOptions options)
    {
        Options = options;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new OpenBaoConfigurationProvider(Options);
    }
}
