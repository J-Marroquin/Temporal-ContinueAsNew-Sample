namespace Temporal.ContinueAsNew.Worker.Models.DTOs;

public record FailedItemDto(string ItemId, string? Message);