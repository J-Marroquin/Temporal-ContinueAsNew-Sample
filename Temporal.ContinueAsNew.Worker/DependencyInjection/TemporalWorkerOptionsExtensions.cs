using Microsoft.Extensions.DependencyInjection;
using Temporal.ContinueAsNew.Worker.Activities.CsvActivities;
using Temporal.ContinueAsNew.Worker.Activities.Inventory;
using Temporal.ContinueAsNew.Worker.Configuration;
using Temporal.ContinueAsNew.Worker.Workflows;
using Temporalio.Worker;

namespace Temporal.ContinueAsNew.Worker.DependencyInjection;

public static class TemporalWorkerOptionsExtensions
{
    public static TemporalWorkerOptions AddWorkflows(
        this TemporalWorkerOptions options,
        IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(options);
        
        var csv = services.GetRequiredService<CsvActivities>();
        var inventory = services.GetRequiredService<InventoryActivities>();

        return options
            // Workflows
            .AddWorkflow<ReserveItemsWorkflow>()
            .AddWorkflow<ProcessItemWorkflow>()
            .AddAllActivities(csv)
            .AddAllActivities(inventory);
    }
    
    public static TemporalWorkerOptions ConfigureOptions(this TemporalWorkerOptions options, TemporalOptions settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.Worker.MaxConcurrentActivities);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.Worker.MaxActivitiesPerSecond);
        
        options.MaxConcurrentActivities = settings.Worker.MaxConcurrentActivities;
        options.MaxActivitiesPerSecond = settings.Worker.MaxActivitiesPerSecond;
        
        return options;
    }
}