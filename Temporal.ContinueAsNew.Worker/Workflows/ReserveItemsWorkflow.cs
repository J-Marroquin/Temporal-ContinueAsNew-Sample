using Microsoft.Extensions.Logging;
using Temporal.ContinueAsNew.Worker.Activities.CsvActivities;
using Temporal.ContinueAsNew.Worker.Models;
using Temporal.ContinueAsNew.Worker.Models.DTOs;
using Temporalio.Api.Enums.V1;
using Temporalio.Common;
using Temporalio.Workflows;

namespace Temporal.ContinueAsNew.Worker.Workflows;

[Workflow]
public class ReserveItemsWorkflow
{
    private const int DefaultMaxConcurrency = 5;

    private int _total;
    private int _processed;
    private int _failed;
    private bool _cancelRequested;

    [WorkflowQuery]
    public ProgressDto GetProgress() => new ProgressDto
    {
        Processed = _processed,
        Failed = _failed,
        Total = _total
    };

    [WorkflowSignal]
    public Task Cancel()
    {
        _cancelRequested = true;
        Workflow.Logger.LogInformation("Cancel signal received");
        return Task.CompletedTask;
    }

    [WorkflowRun]
    public async Task<ProcessResponse> RunAsync(ReserveItemsInput input)
    {
        _processed = input.Processed;
        _failed = input.Failed;

        string dataLocation = input.DataLocation!;

        // Initial run: download, parse, and persist CSV data
        if (input.DataLocation == null)
        {
            Workflow.Logger.LogInformation("Initial run: downloading and parsing CSV");

            var csv = await Workflow.ExecuteActivityAsync(
                (ICsvActivities a) => a.DownloadCsvAsync(input.CsvPath),
                new ActivityOptions
                {
                    StartToCloseTimeout = TimeSpan.FromSeconds(30),
                    RetryPolicy = new RetryPolicy
                    {
                        MaximumAttempts = 3
                    }
                });

            var rows = await Workflow.ExecuteActivityAsync(
                (ICsvActivities a) => a.ParseCsvAsync(csv),
                new ActivityOptions
                {
                    StartToCloseTimeout = TimeSpan.FromSeconds(30)
                });

            dataLocation = await Workflow.ExecuteActivityAsync(
                (ICsvActivities a) => a.SaveRowsAsync(rows),
                new ActivityOptions
                {
                    StartToCloseTimeout = TimeSpan.FromSeconds(30),
                    RetryPolicy = new RetryPolicy
                    {
                        MaximumAttempts = 2
                    }
                });

            _total = rows.Count;
        }
        else
        {
            _total = input.Total;
        }

        // Retrieve batch from persisted storage
        var batch = await Workflow.ExecuteActivityAsync(
            (ICsvActivities a) => a.GetRowsBatchAsync(
                dataLocation,
                input.CurrentIndex,
                input.BatchSize),
            new ActivityOptions
            {
                StartToCloseTimeout = TimeSpan.FromSeconds(30),
                RetryPolicy = new RetryPolicy
                {
                    MaximumAttempts = 3
                }
            });

        Workflow.Logger.LogInformation("Processing batch size {Count}", batch.Count);

        var semaphore = new Temporalio.Workflows.Semaphore(DefaultMaxConcurrency);
        var tasks = new List<Task>();

        foreach (var row in batch)
        {
            // Stop scheduling new work if cancellation was requested
            if (_cancelRequested)
            {
                Workflow.Logger.LogInformation("Cancellation requested. Stopping scheduling of new child workflows.");
                break;
            }
            
            // Acquire a slot before scheduling a new child workflow
            await semaphore.WaitAsync();

            var item = new OrderItem(row.ItemId, row.Quantity);

            var task = Workflow.RunTaskAsync(async () =>
            {
                try
                {
                    // Execute child workflow for each item
                    await Workflow.ExecuteChildWorkflowAsync(
                        (ProcessItemWorkflow wf) => wf.RunAsync(item),
                        new ChildWorkflowOptions
                        {
                            Id = $"process-item-{row.ItemId}-{input.ImportId}",
                            IdReusePolicy = WorkflowIdReusePolicy.RejectDuplicate
                        });
                }
                finally
                {
                    // Always release the semaphore slot
                    semaphore.Release();
                }
            });

            tasks.Add(task);
        }

        // Wait for all already-started child workflows to complete
        await Task.WhenAll(tasks);

        // Only count processed items that were actually scheduled
        var scheduledCount = tasks.Count;
        _processed += scheduledCount;

        var nextIndex = input.CurrentIndex + scheduledCount;

        // Continue-As-New if there are more items to process
        if (nextIndex < _total || Workflow.ContinueAsNewSuggested)
        {
            Workflow.Logger.LogInformation(
                "Continue-As-New triggered. NextIndex={Index}, Suggested={Suggested}",
                nextIndex,
                Workflow.ContinueAsNewSuggested);
            
            var nextInput = input with
            {
                DataLocation = dataLocation,
                CurrentIndex = nextIndex,
                Processed = _processed,
                Failed = _failed,
                Total = _total
            };

            throw Workflow.CreateContinueAsNewException(
                (ReserveItemsWorkflow wf) => wf.RunAsync(nextInput));
        }

        // Final cleanup after all batches are processed
        Workflow.Logger.LogInformation("Final run: cleaning up folder");

        await Workflow.ExecuteActivityAsync(
            (ICsvActivities a) => a.CleanupAsync(dataLocation),
            new ActivityOptions
            {
                StartToCloseTimeout = TimeSpan.FromSeconds(30)
            });

        Workflow.Logger.LogInformation("Workflow completed");
        
        return new ProcessResponse(Code: 0);
    }
}