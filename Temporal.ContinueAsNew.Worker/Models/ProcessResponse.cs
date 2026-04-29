using System.Text.Json.Serialization;

namespace Temporal.ContinueAsNew.Worker.Models;

public record ProcessResponse(
    [property: JsonPropertyName("code")] 
    ProcessResponseCode Code,
    string? Message = null);

public enum ProcessResponseCode
{
    Success,
    Failed,
}    