using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Ptax.Data.Tests.Base;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:16-alpine").Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public PtaxDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PtaxDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        return new PtaxDbContext(options);
    }

    public async Task DisposeAsync()
        => await _container.DisposeAsync();
}