using Ptax.Bcb;

namespace Ptax.Worker;

public static class ServiceCollectionExtensions
{
    public static IHttpClientBuilder AddPtaxClient(this IServiceCollection services)
    {
        var builder = services.AddHttpClient<IPtaxClient, BcbPtaxClient>();

        builder.AddStandardResilienceHandler();

        return builder;
    }
}