namespace Temporal.ContinueAsNew.Worker.Models;

public record TokenCacheEntry(string AccessToken, DateTime ExpiresAt);