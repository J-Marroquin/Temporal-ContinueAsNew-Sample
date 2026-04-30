namespace Temporal.ContinueAsNew.Worker.Sevices;

public interface ITokenProvider
{
    Task<string> GetIdentityAccessToken(int tenantId, CancellationToken cancellationToken = default);
}