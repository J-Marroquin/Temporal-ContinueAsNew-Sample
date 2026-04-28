using Microsoft.Extensions.Logging;
using Temporal.ContinueAsNew.Worker.Activities.Inventory;
using Temporal.ContinueAsNew.Worker.Models;
using Temporalio.Common;
using Temporalio.Workflows;

namespace Temporal.ContinueAsNew.Worker.Workflows;

[Workflow]
public class ProcessItemWorkflow
{
    [WorkflowRun]
    public async Task RunAsync(OrderItem item)
    {
        Workflow.Logger.LogInformation("Processing item {Id}", item.ItemId);
        
        var isAvailable = await Workflow.ExecuteActivityAsync<IInventoryActivities, bool>(
            a => a.CheckAvailabilityAsync(item.ItemId),
            new ActivityOptions
            {
                StartToCloseTimeout = TimeSpan.FromSeconds(30),
                RetryPolicy = new RetryPolicy
                {
                    MaximumAttempts = 4,
                    InitialInterval = TimeSpan.FromSeconds(2),
                    BackoffCoefficient = 2,
                    MaximumInterval = TimeSpan.FromSeconds(10),
                }
            });
        
        if (!isAvailable)
        {
            Workflow.Logger.LogInformation("Item {Id} not available on Stock", item.ItemId);
            return;
        }
        
        await Workflow.ExecuteActivityAsync(
            (IInventoryActivities a) => a.ReserveItemAsync(item),
            new ActivityOptions
            {
                StartToCloseTimeout = TimeSpan.FromSeconds(30),
                RetryPolicy = new RetryPolicy
                {
                    MaximumAttempts = 3,
                    InitialInterval = TimeSpan.FromSeconds(2),
                    BackoffCoefficient = 2,
                    MaximumInterval = TimeSpan.FromSeconds(12),
                }
            });

    }
}