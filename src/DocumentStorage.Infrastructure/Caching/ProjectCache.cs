using DocumentStorage.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace DocumentStorage.Infrastructure.Caching;

public class ProjectCache : IProjectCache
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public ProjectCache(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task<Guid> GetOrCreateAsync(
        string apiKey,
        Func<CancellationToken, Task<Guid>> factory,
        CancellationToken ct = default)
    {
        return await _cache.GetOrCreateAsync($"project:{apiKey}", async entry =>
        {
            entry.SetSlidingExpiration(CacheDuration);
            return await factory(ct);
        });
    }

    public void Invalidate(string apiKey)
    {
        _cache.Remove($"project:{apiKey}");
    }
}
