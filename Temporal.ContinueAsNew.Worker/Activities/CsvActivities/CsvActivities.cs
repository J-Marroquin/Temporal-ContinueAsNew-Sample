using System.Text.Json;
using Temporal.ContinueAsNew.Worker.Models.DTOs;
using Temporalio.Activities;

namespace Temporal.ContinueAsNew.Worker.Activities.CsvActivities;

public class CsvActivities : ICsvActivities
{
    [Activity]
    public async Task<string> DownloadCsvAsync(string path)
    {
        return await File.ReadAllTextAsync(path);
    }
    
    [Activity]
    public Task<List<RowDto>> ParseCsvAsync(string csvContent)
    {
        var lines = csvContent
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Skip(1); // skip header

        var rows = lines.Select(line =>
        {
            var parts = line.Split(',');

            return new RowDto
            {
                ItemId = parts[0],
                Quantity = int.Parse(parts[1]),
            };
        }).ToList();

        return Task.FromResult(rows);
    }
    
    [Activity]
    public async Task<string> SaveRowsAsync(List<RowDto> rows)
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "csv-data", Guid.NewGuid().ToString());

        Directory.CreateDirectory(folder);

        foreach (var row in rows)
        {
            var path = Path.Combine(folder, $"row-{row.ItemId}.json");

            var json = JsonSerializer.Serialize(row);

            await File.WriteAllTextAsync(path, json);
        }

        return folder;
    }
    
    [Activity]
    public async Task<List<RowDto>> GetRowsBatchAsync(string folderPath, int start, int? size)
    {
        var filesQuery = Directory
            .GetFiles(folderPath, "*.json")
            .OrderBy(f => f)
            .Skip(start);
        
        var files = size.HasValue
            ? filesQuery.Take(size.Value).ToList()
            : filesQuery.ToList();
        
        var result = new List<RowDto>();

        foreach (var file in files)
        {
            var json = await File.ReadAllTextAsync(file);
            var row = JsonSerializer.Deserialize<RowDto>(json);

            if (row != null)
                result.Add(row);
        }

        return result;
    }
    
    [Activity]
    public Task CleanupAsync(string folderPath)
    {
        if (Directory.Exists(folderPath))
            Directory.Delete(folderPath, recursive: true);

        return Task.CompletedTask;
    }
}