using Microsoft.Extensions.Logging;
using Temporal.ContinueAsNew.Worker.Activities.Inventory;
using Temporal.ContinueAsNew.Worker.Models;
using Temporal.ContinueAsNew.Worker.Models.DTOs;
using Temporalio.Common;
using Temporalio.Workflows;

namespace Temporal.ContinueAsNew.Worker.Workflows;

[Workflow]
public class ProcessItemWorkflow
{
    [WorkflowRun]
    public async Task<ProcessResponse> RunAsync(OrderItem item)
    {
        Workflow.Logger.LogInformation("Processing item {Id}", item.ItemId);
        
        var isAvailable = await Workflow.ExecuteActivityAsync<IInventoryActivities, bool>(
            a => a.CheckAvailabilityAsync(item),
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
            return new ProcessResponse(ProcessResponseCode.Failed, "Item not available on Stock");
        }
        
        var reserveItemResponse = await Workflow.ExecuteActivityAsync<IInventoryActivities, ReserveItemResponseDto>(
            a => a.ReserveItemAsync(item),
            new ActivityOptions
            {
                StartToCloseTimeout = TimeSpan.FromSeconds(30),
                RetryPolicy = new RetryPolicy
                {
                    MaximumAttempts = 5,
                    InitialInterval = TimeSpan.FromSeconds(2),
                    BackoffCoefficient = 2,
                    MaximumInterval = TimeSpan.FromSeconds(12),
                }
            });
        
        
        return reserveItemResponse.IsSuccess 
            ? new ProcessResponse(ProcessResponseCode.Success) 
            : new ProcessResponse(ProcessResponseCode.Failed, reserveItemResponse.ErrorMessage);
    }
}