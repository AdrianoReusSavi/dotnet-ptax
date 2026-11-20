using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Ptax.Data;
using Ptax.Worker.Tests.Fakes;

namespace Ptax.Worker.Tests;

public class PtaxSyncWorkerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 3, 15, 0, 0, TimeSpan.Zero);

    private readonly FakePtaxSyncService _service = new();
    private readonly FakeRateAlertService _alerts = new();
    private readonly FakeLogger<PtaxSyncWorker> _logger = new();
    private int _scopes;
    private decimal _thresholdPercent = 1m;

    private static RateAlert CreateAlert(string currency, DateOnly date) => new()
    {
        Currency = currency,
        PreviousDate = date.AddDays(-1),
        Date = date,
        PreviousSellRate = 5m,
        SellRate = 5.1m,
        ChangePercent = 1m,
    };

    private PtaxSyncWorker CreateWorker(DateTimeOffset now, params string[] currencies)
    {
        var provider = new ServiceCollection()
            .AddScoped<IPtaxSyncService>(_ => { _scopes++; return _service; })
            .AddScoped<IRateAlertService>(_ => _alerts)
            .BuildServiceProvider();

        return new PtaxSyncWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new PtaxSyncOptions { Currencies = currencies, WindowDays = 7, AlertThresholdPercent = _thresholdPercent }),
            new FakeTimeProvider(now),
            _logger);
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

    [Fact]
    public async Task RunOnce_Checks_Alerts_With_Sync_Window_And_Threshold()
    {
        _thresholdPercent = 2.5m;

        var worker = CreateWorker(Now, "EUR");

        await worker.RunOnceAsync(CancellationToken.None);

        var sync = Assert.Single(_service.Calls);
        var check = Assert.Single(_alerts.Calls);

        Assert.Equal("EUR", check.Currency);
        Assert.Equal(sync.From, check.From);
        Assert.Equal(sync.To, check.To);
        Assert.Equal(2.5m, check.ThresholdPercent);
    }

    [Fact]
    public async Task RunOnce_Checks_Alerts_After_Sync()
    {
        _alerts.Events = _service.Events;

        var worker = CreateWorker(Now, "USD", "EUR");

        await worker.RunOnceAsync(CancellationToken.None);

        Assert.Equal(["sync:USD", "alerts:USD", "sync:EUR", "alerts:EUR"], _service.Events);
    }

    [Fact]
    public async Task RunOnce_Logs_Warning_For_Each_New_Alert()
    {
        _alerts.Results["USD"] = [CreateAlert("USD", new DateOnly(2026, 11, 02)), CreateAlert("USD", new DateOnly(2026, 11, 03))];

        var worker = CreateWorker(Now, "USD", "EUR");

        await worker.RunOnceAsync(CancellationToken.None);

        var warnings = _logger.Collector.GetSnapshot().Where(r => r.Level == LogLevel.Warning);

        Assert.Equal(2, warnings.Count());
    }

    [Fact]
    public async Task RunOnce_Sync_Failure_Skips_Alert_Check()
    {
        _service.Failures["USD"] = new HttpRequestException();

        var worker = CreateWorker(Now, "USD", "EUR");

        await worker.RunOnceAsync(CancellationToken.None);

        Assert.Equal(["EUR"], _alerts.Calls.Select(c => c.Currency));
    }

    [Fact]
    public async Task RunOnce_Alert_Failure_Does_Not_Stop_Others()
    {
        _alerts.Failures["USD"] = new InvalidOperationException();

        var worker = CreateWorker(Now, "USD", "EUR");

        await worker.RunOnceAsync(CancellationToken.None);

        Assert.Equal(["USD", "EUR"], _service.Calls.Select(c => c.Currency));
        Assert.Equal(["USD", "EUR"], _alerts.Calls.Select(c => c.Currency));

        var errors = _logger.Collector.GetSnapshot().Where(r => r.Level == LogLevel.Error);

        Assert.Single(errors);
    }
}