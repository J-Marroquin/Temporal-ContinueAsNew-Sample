namespace Temporal.ContinueAsNew.Worker.Models;

public record OrderItem(
    string ItemId,
    int RequestedQuantity,
    int TenantId
);