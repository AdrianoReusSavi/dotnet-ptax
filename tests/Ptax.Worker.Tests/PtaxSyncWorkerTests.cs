using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Ptax.Worker.Tests.Fakes;

namespace Ptax.Worker.Tests;

public class PtaxSyncWorkerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 3, 15, 0, 0, TimeSpan.Zero);

    private readonly FakePtaxSyncService _service = new();
    private int _scopes;

    private PtaxSyncWorker CreateWorker(DateTimeOffset now, params string[] currencies)
    {
        var provider = new ServiceCollection()
            .AddScoped<IPtaxSyncService>(_ => { _scopes++; return _service; })
            .BuildServiceProvider();

        return new PtaxSyncWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new PtaxSyncOptions { Currencies = currencies, WindowDays = 7 }),
            new FakeTimeProvider(now),
            NullLogger<PtaxSyncWorker>.Instance);
    }

    [Fact]
    public async Task RunOnce_Syncs_All_Configured_Currencies_In_Order()
    {
        var worker = CreateWorker(Now, "USD", "EUR");

        await worker.RunOnceAsync(CancellationToken.None);

        Assert.Equal(["USD", "EUR"], _service.Calls.Select(call => call.Currency));
    }

    [Fact]
    public async Task RunOnce_Uses_Utc_Today_As_End_Of_Window()
    {
        var now = new DateTimeOffset(2026, 11, 3, 22, 30, 0, TimeSpan.FromHours(-3));

        var worker = CreateWorker(now, "USD");

        await worker.RunOnceAsync(CancellationToken.None);

        var call = Assert.Single(_service.Calls);

        Assert.Equal(new DateOnly(2026, 10, 28), call.From);
        Assert.Equal(new DateOnly(2026, 11, 4), call.To);
    }

    [Fact]
    public async Task RunOnce_Creates_One_Scope_Per_Currency()
    {
        var worker = CreateWorker(Now, "USD", "EUR", "BRL");

        await worker.RunOnceAsync(CancellationToken.None);

        Assert.Equal(3, _scopes);
    }

    [Fact]
    public async Task RunOnce_Currency_Failure_Does_Not_Stop_Others()
    {
        _service.Failures["USD"] = new Exception("Failed to sync USD");

        var worker = CreateWorker(Now, "USD", "EUR", "BRL");

        await worker.RunOnceAsync(CancellationToken.None);

        Assert.Equal(["USD", "EUR", "BRL"], _service.Calls.Select(call => call.Currency));
    }

    [Fact]
    public async Task RunOnce_Timeout_Without_Shutdown_Does_Not_Stop_Others()
    {
        _service.Failures["USD"] = new TaskCanceledException();

        var worker = CreateWorker(Now, "USD", "EUR", "BRL");

        await worker.RunOnceAsync(CancellationToken.None);

        Assert.Equal(["USD", "EUR", "BRL"], _service.Calls.Select(call => call.Currency));
    }

    [Fact]
    public async Task RunOnce_Shutdown_Stops_Before_Syncing()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var worker = CreateWorker(Now, "USD");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            worker.RunOnceAsync(cts.Token));

        Assert.Empty(_service.Calls);
    }
}