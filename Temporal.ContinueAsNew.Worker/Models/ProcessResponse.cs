using System.Text.Json.Serialization;

namespace Temporal.ContinueAsNew.Worker.Models;

public record ProcessResponse(
    [property: JsonPropertyName("code")] int Code);