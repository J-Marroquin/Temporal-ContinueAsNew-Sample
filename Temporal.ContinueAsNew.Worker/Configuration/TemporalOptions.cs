namespace Temporal.ContinueAsNew.Worker.Configuration;

public class TemporalOptions
{
    public string Host { get; set; } = null!;
    public string TaskQueue { get; set; } = null!;
    public WorkerOptions Worker  { get; set; } = null!;
}

public class WorkerOptions
{
    public int MaxConcurrentActivities { get; set; }
    public int MaxActivitiesPerSecond { get; set; }
}