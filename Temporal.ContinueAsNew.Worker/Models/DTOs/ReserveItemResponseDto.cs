namespace Temporal.ContinueAsNew.Worker.Models.DTOs;

public class ReserveItemResponseDto
{
    public bool IsSuccess { get; set; }
    public int? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    
}