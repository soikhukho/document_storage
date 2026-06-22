namespace DocumentStorage.Application.Interfaces;

/// <summary>
/// Caches project lookups by API key to avoid hitting the database on every request.
/// </summary>
public interface IProjectCache
{
    Task<Guid> GetOrCreateAsync(string apiKey, Func<CancellationToken, Task<Guid>> factory, CancellationToken ct = default);

    void Invalidate(string apiKey);
}
