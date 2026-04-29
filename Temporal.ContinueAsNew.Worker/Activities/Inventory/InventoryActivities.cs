using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Temporal.ContinueAsNew.Worker.Models;
using Temporal.ContinueAsNew.Worker.Models.DTOs;
using Temporalio.Activities;
using Temporalio.Exceptions;

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
    public async Task<ReserveItemResponseDto> ReserveItemAsync(OrderItem orderItem)
    {
        _logger.LogInformation("Reserving item {ItemId}", orderItem.ItemId);

        var response = await _http.PostAsJsonAsync("reserve", orderItem);

        if (!response.IsSuccessStatusCode)
        {
            var statusCode = (int)response.StatusCode;

            return statusCode switch
            {
                400 => (await response.Content.ReadFromJsonAsync<ReserveItemResponseDto>())!,
                > 400 and < 500 => throw new 
                    ApplicationFailureException(
                        $"Non-retryable error reserving item {orderItem.ItemId}. StatusCode: {statusCode}", 
                        "NonRetryableHttpError", nonRetryable: true),
                _ => throw new 
                    ApplicationFailureException(
                        $"Transient error reserving item {orderItem.ItemId}. StatusCode: {statusCode}", 
                        "TransientHttpError", nonRetryable: false)
            };
        }

        var result = await response.Content.ReadFromJsonAsync<ReserveItemResponseDto>();

        if (result == null)
            throw new InvalidCastException("Empty response from reserve endpoint");
        
        return result;
        
    }
}