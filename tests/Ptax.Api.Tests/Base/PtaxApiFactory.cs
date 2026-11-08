using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Ptax.Data;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace Ptax.Api.Tests.Base;

public sealed class PtaxApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine").Build();
    private readonly RedisContainer _redis = new RedisBuilder("redis:7-alpine").Build();

    public FakeTimeProvider TimeProvider { get; } = new(new DateTimeOffset(2026, 11, 3, 15, 0, 0, TimeSpan.Zero));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Ptax", _container.GetConnectionString());
        builder.UseSetting("ConnectionStrings:Redis", _redis.GetConnectionString());

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(TimeProvider);
        });
    }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_container.StartAsync(), _redis.StartAsync());

        using var scope = Services.CreateScope();

        await scope.ServiceProvider.GetRequiredService<PtaxDbContext>().Database.MigrateAsync();
    }

    public async Task SeedAsync(params DailyRate[] rates)
    {
        using var scope = Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<PtaxDbContext>();

        await new DailyRateRepository(context).UpsertAsync(rates);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _container.DisposeAsync();

        await _redis.DisposeAsync();

        await base.DisposeAsync();
    }
}