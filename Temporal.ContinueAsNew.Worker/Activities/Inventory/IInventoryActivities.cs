using Temporal.ContinueAsNew.Worker.Models;
using Temporalio.Activities;

namespace Temporal.ContinueAsNew.Worker.Activities.Inventory;

public interface IInventoryActivities
{
    [Activity]
    Task<bool> CheckAvailabilityAsync(string itemId);
    [Activity]
    Task ReserveItemAsync(OrderItem orderItem);
}