using Microsoft.Extensions.Caching.Memory;

namespace RepairSystem.Web.Services.Authentication;

public sealed record ApiTokenSession(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    DateTimeOffset SessionExpiresAtUtc,
    string? RefreshToken = null,
    DateTimeOffset? RefreshTokenExpiresAtUtc = null);

public interface IApiTokenSessionStore
{
    void Save(string sessionId, ApiTokenSession session);

    bool TryGet(
        string sessionId,
        out ApiTokenSession? session);

    void Remove(string sessionId);
}

public sealed class MemoryApiTokenSessionStore : IApiTokenSessionStore
{
    private readonly IMemoryCache _cache;

    public MemoryApiTokenSessionStore(IMemoryCache cache)
    {
        _cache = cache;
    }

    public void Save(string sessionId, ApiTokenSession session)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentNullException.ThrowIfNull(session);

        if (session.SessionExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            throw new ArgumentException(
                "The session expiry must be in the future.",
                nameof(session));
        }

        _cache.Set(
            GetCacheKey(sessionId),
            session,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = session.SessionExpiresAtUtc
            });
    }

    public bool TryGet(
        string sessionId,
        out ApiTokenSession? session)
    {
        if (!string.IsNullOrWhiteSpace(sessionId) &&
            _cache.TryGetValue(
                GetCacheKey(sessionId),
                out ApiTokenSession? cachedSession) &&
            cachedSession is not null &&
            cachedSession.SessionExpiresAtUtc > DateTimeOffset.UtcNow)
        {
            session = cachedSession;
            return true;
        }

        session = null;
        return false;
    }

    public void Remove(string sessionId)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            _cache.Remove(GetCacheKey(sessionId));
        }
    }

    private static string GetCacheKey(string sessionId)
        => $"repair-api-session:{sessionId}";
}