using DocumentStorage.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace DocumentStorage.Infrastructure.Caching;

public class ProjectCache : IProjectCache
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan PositiveCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan NegativeCacheDuration = TimeSpan.FromSeconds(30);

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
            entry.SetSize(1);
            var projectId = await factory(ct).ConfigureAwait(false);

            if (projectId == Guid.Empty)
                entry.SetAbsoluteExpiration(NegativeCacheDuration);
            else
                entry.SetSlidingExpiration(PositiveCacheDuration);

            return projectId;
        }).ConfigureAwait(false);
    }

    public void Invalidate(string apiKey)
    {
        _cache.Remove($"project:{apiKey}");
    }
}
