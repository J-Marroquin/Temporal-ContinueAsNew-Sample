using Temporal.ContinueAsNew.Worker.Models;
using Temporal.ContinueAsNew.Worker.Models.DTOs;
using Temporalio.Activities;

namespace Temporal.ContinueAsNew.Worker.Activities.Inventory;

public interface IInventoryActivities
{
    [Activity]
    Task<bool> CheckAvailabilityAsync(string itemId);
    [Activity]
    Task<ReserveItemResponseDto> ReserveItemAsync(OrderItem orderItem);
}