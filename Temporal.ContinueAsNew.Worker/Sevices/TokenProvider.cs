using IdentityModel.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Temporal.ContinueAsNew.Worker.Configuration;
using Temporal.ContinueAsNew.Worker.Models;

namespace Temporal.ContinueAsNew.Worker.Sevices;

public class TokenProvider : ITokenProvider
{
    private readonly ILogger<TokenProvider> _logger;
    private readonly ITokenCache _tokenCache;
    private readonly IMemoryCache _memoryCache;
    private readonly HttpClient _httpClient;
    private readonly IdentityOptions _identity;

    private static readonly SemaphoreSlim Lock = new(1, 1);

    private const string DiscoveryCacheKey = "identity:discovery";

    public TokenProvider(
        ILogger<TokenProvider> logger,
        ITokenCache tokenCache,
        IMemoryCache memoryCache,
        IOptions<IdentityOptions> identity,
        HttpClient httpClient)
    {
        _logger = logger;
        _tokenCache = tokenCache;
        _memoryCache = memoryCache;
        _identity = identity.Value;
        _httpClient = httpClient;

        _httpClient.BaseAddress = new Uri(_identity.IdentityAuthorityUrl);
    }

    public async Task<string> GetIdentityAccessToken(
        int tenantId,
        CancellationToken cancellationToken = default)
    {
        var key = $"token:{tenantId}";

        // 1. Try cache
        if (_tokenCache.TryGet(key, out var cached))
        {
            _logger.LogInformation("Returning cached token for tenant {TenantId} | ExpiresAt: {ExpiresAt:O}",
                tenantId, cached!.ExpiresAt);
            return cached.AccessToken;
        }

        await Lock.WaitAsync(cancellationToken);
        try
        {
            // double-check
            if (_tokenCache.TryGet(key, out cached))
            {
                _logger.LogInformation("Returning cached token for tenant {TenantId} | ExpiresAt: {ExpiresAt:O}",
                    tenantId, cached!.ExpiresAt);
                return cached.AccessToken;
            }

            var (token, expiresIn) = await RequestTokenAsync(cancellationToken);

            var expiresAt = DateTime.UtcNow.AddSeconds(expiresIn - 60);

            _tokenCache.Set(key, new TokenCacheEntry(token, expiresAt), expiresAt);

            return token;
        }
        finally
        {
            Lock.Release();
        }
    }
    
    private async Task<DiscoveryDocumentResponse?> GetDiscoveryAsync(CancellationToken ct)
    {
        return await _memoryCache.GetOrCreateAsync<DiscoveryDocumentResponse>(DiscoveryCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);

            var response = await _httpClient.GetDiscoveryDocumentAsync(
                new DiscoveryDocumentRequest
                {
                    Policy =
                    {
                        ValidateIssuerName = false,
                        ValidateEndpoints = false,
                        RequireHttps = false
                    }
                },
                ct);

            if (response.IsError)
                throw new Exception($"Discovery error: {response.Error}");

            return response;
        });
    }

    private async Task<(string accessToken, int expiresIn)> RequestTokenAsync(CancellationToken ct)
    {
        var discovery = await GetDiscoveryAsync(ct);
        if(discovery is null)
            throw new InvalidOperationException("No discovery available");

        var tokenResponse = await _httpClient.RequestClientCredentialsTokenAsync(
            new ClientCredentialsTokenRequest
            {
                Address = discovery.TokenEndpoint,
                ClientId = _identity.ClientId,
                ClientSecret = _identity.ClientSecret,
                Scope = "service1api service2api"
            },
            ct);

        if (tokenResponse.IsError || tokenResponse.AccessToken == null)
            throw new Exception($"Token error: {tokenResponse.Error}");

        return (tokenResponse.AccessToken, tokenResponse.ExpiresIn);
    }
}