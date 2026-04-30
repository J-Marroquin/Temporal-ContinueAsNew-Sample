using Temporal.ContinueAsNew.Worker.Models;

namespace Temporal.ContinueAsNew.Worker.Sevices;

public interface ITokenCache
{
    bool TryGet(string key, out TokenCacheEntry? entry);
    void Set(string key, TokenCacheEntry entry, DateTime absoluteExpiration);
}