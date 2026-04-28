using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Temporal.ContinueAsNew.Worker.Models;
using Temporalio.Activities;

namespace Temporal.ContinueAsNew.Worker.Activities.Inventory;

public class InventoryActivities : IInventoryActivities
{
    private readonly HttpClient _http;
    private readonly ILogger<InventoryActivities> _logger;
    
    public InventoryActivities(HttpClient http, ILogger<InventoryActivities> logger)
    {
        _http = http;
        _logger = logger;
    }
    
    [Activity]
    public async Task<bool> CheckAvailabilityAsync(string itemId)
    {
        _logger.LogInformation("Checking availability for {ItemId}", itemId);

        var product = await _http.GetFromJsonAsync<Product>($"products?itemid={itemId}");

        return product?.OnStock > 0;
    }
    
    [Activity]
    public async Task ReserveItemAsync(OrderItem orderItem)
    {
        _logger.LogInformation("Reserving item {ItemId}", orderItem.ItemId);

        var response = await _http.PostAsJsonAsync("reserve", orderItem);
        response.EnsureSuccessStatusCode();
    }
}