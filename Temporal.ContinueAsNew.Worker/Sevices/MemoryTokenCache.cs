using Microsoft.Extensions.Caching.Memory;
using Temporal.ContinueAsNew.Worker.Models;

namespace Temporal.ContinueAsNew.Worker.Sevices;

public class MemoryTokenCache : ITokenCache
{
    private readonly IMemoryCache _cache;
    
    public MemoryTokenCache(IMemoryCache cache)
    {
        _cache = cache;
    }

    public bool TryGet(string key, out TokenCacheEntry? entry)
    {
        if (_cache.TryGetValue<TokenCacheEntry>(key, out var value))
        {
            if (DateTime.UtcNow < value!.ExpiresAt)
            {
                entry = value;
                return true;
            }

            _cache.Remove(key);
        }

        entry = null;
        return false;
    }
    
    public void Set(string key, TokenCacheEntry entry, DateTime absoluteExpiration)
    {
        _cache.Set(key, entry, new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = absoluteExpiration
        });
    }
}