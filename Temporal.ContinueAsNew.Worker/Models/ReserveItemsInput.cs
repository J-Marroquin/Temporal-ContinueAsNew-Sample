namespace Temporal.ContinueAsNew.Worker.Models;

public record ReserveItemsInput(
    string ImportId,
    string OrderId,
    string CsvPath,
    string? DataLocation = null,
    int CurrentIndex = 0,
    int? BatchSize = null,
    int Processed = 0,
    int Total = 0);