using Microsoft.Extensions.Caching.Distributed;

namespace Ptax.Api.Tests.Fakes;

public class FakeDistributedCache : IDistributedCache
{
    public Dictionary<string, byte[]> Entries { get; } = [];
    public Dictionary<string, DistributedCacheEntryOptions> Options { get; } = [];
    public Exception? ExceptionToThrow { get; set; }
    public int Calls { get; private set; }

    public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        Calls++;

        token.ThrowIfCancellationRequested();

        if (ExceptionToThrow is not null)
            throw ExceptionToThrow;

        return Task.FromResult(Entries.GetValueOrDefault(key));
    }

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        Calls++;

        token.ThrowIfCancellationRequested();

        if (ExceptionToThrow is not null)
            throw ExceptionToThrow;

        Entries[key] = value;
        Options[key] = options;

        return Task.CompletedTask;
    }

    public Task RefreshAsync(string key, CancellationToken token = default)
        => Task.CompletedTask;

    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        Entries.Remove(key);

        return Task.CompletedTask;
    }

    public byte[]? Get(string key)
        => throw new NotSupportedException();

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        => throw new NotSupportedException();

    public void Refresh(string key)
        => throw new NotSupportedException();

    public void Remove(string key)
        => throw new NotSupportedException();
}