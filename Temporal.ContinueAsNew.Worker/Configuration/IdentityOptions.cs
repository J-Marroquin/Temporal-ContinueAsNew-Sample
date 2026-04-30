namespace Temporal.ContinueAsNew.Worker.Configuration;

public class IdentityOptions
{
    public string IdentityAuthorityUrl { get; set; } = null!;
    public string ClientId { get; set; } = null!;
    public string ClientSecret { get; set; } = null!;
}