using Temporal.ContinueAsNew.Worker.Models.DTOs;
using Temporalio.Activities;

namespace Temporal.ContinueAsNew.Worker.Activities.CsvActivities;

public interface ICsvActivities
{
    [Activity]
    Task<string> DownloadCsvAsync(string path);
    [Activity]
    Task<List<RowDto>> ParseCsvAsync(string csvContent);
    [Activity]
    Task<string> SaveRowsAsync(List<RowDto> rows);
    [Activity]
    Task<List<RowDto>> GetRowsBatchAsync(string folderPath, int start, int size);
    [Activity]
    Task CleanupAsync(string folderPath);
}