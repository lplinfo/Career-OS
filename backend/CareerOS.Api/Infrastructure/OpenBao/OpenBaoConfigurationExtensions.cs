using Microsoft.Extensions.Configuration;

namespace CareerOS.Api.Infrastructure.OpenBao;

public static class OpenBaoConfigurationExtensions
{
    public static IConfigurationBuilder AddOpenBao(this IConfigurationBuilder builder, OpenBaoOptions options)
    {
        return builder.Add(new OpenBaoConfigurationSource(options));
    }
}
