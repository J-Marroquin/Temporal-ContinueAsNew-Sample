namespace Temporal.ContinueAsNew.Worker.Models.DTOs;

public class ProgressDto
{
    public int Processed { get; set; }
    public int Failed { get; set; }
    public int Total { get; set; }
}