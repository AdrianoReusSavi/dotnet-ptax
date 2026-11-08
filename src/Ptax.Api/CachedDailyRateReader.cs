using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Ptax.Data;

namespace Ptax.Api;

public class CachedDailyRateReader(
    IDailyRateReader inner,
    IDistributedCache cache,
    ILogger<CachedDailyRateReader> logger) : IDailyRateReader
{
    private static readonly TimeSpan ExactDateExpiration = TimeSpan.FromDays(1);
    private static readonly TimeSpan FallbackExpiration = TimeSpan.FromMinutes(5);

    public async Task<DailyRate?> GetLatestOnOrBeforeAsync(string currency, DateOnly date, CancellationToken cancellationToken = default)
    {
        var key = $"rate:{currency}:{date:yyyy-MM-dd}";

        try
        {
            var cached = await cache.GetStringAsync(key, cancellationToken);

            if (cached is not null)
                return JsonSerializer.Deserialize<DailyRate>(cached);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Cache read failed for {Key}", key);
        }

        var rate = await inner.GetLatestOnOrBeforeAsync(currency, date, cancellationToken);

        if (rate is not null)
        {
            try
            {
                var expirationTime = rate.Date == date ? ExactDateExpiration : FallbackExpiration;

                await cache.SetStringAsync(key, JsonSerializer.Serialize(rate),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expirationTime }, cancellationToken);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Cache write failed for {Key}", key);
            }
        }

        return rate;
    }

    public Task<IReadOnlyList<DailyRate>> GetRangeAsync(string currency, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
        => inner.GetRangeAsync(currency, from, to, cancellationToken);
}