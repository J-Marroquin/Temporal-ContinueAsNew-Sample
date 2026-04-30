using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Temporal.ContinueAsNew.Worker.Models;
using Temporal.ContinueAsNew.Worker.Models.DTOs;
using Temporal.ContinueAsNew.Worker.Sevices;
using Temporalio.Activities;
using Temporalio.Exceptions;

namespace Temporal.ContinueAsNew.Worker.Activities.Inventory;

public class InventoryActivities : IInventoryActivities
{
    private readonly HttpClient _http;
    private readonly ILogger<InventoryActivities> _logger;
    private readonly ITokenProvider _tokenProvider;
    
    public InventoryActivities(
        HttpClient http, 
        ITokenProvider tokenProvider,
        ILogger<InventoryActivities> logger)
    {
        _http = http;
        _logger = logger;
        _tokenProvider = tokenProvider;
    }
    
    [Activity]
    public async Task<bool> CheckAvailabilityAsync(OrderItem orderItem)
    {
        _logger.LogInformation("Checking availability for ItemId: {ItemId}", orderItem.ItemId);
        
        var token = await _tokenProvider.GetIdentityAccessToken(orderItem.TenantId);
        
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"products?itemid={orderItem.ItemId}");
        
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
        var response = await _http.SendAsync(request);
        
        var product = await response.Content.ReadFromJsonAsync<Product>();

        return product?.OnStock > 0;
    }
    
    [Activity]
    public async Task<ReserveItemResponseDto> ReserveItemAsync(OrderItem orderItem)
    {
        _logger.LogInformation("Reserving item {ItemId}", orderItem.ItemId);

        var token = await _tokenProvider.GetIdentityAccessToken(orderItem.TenantId);
        
        var request = new HttpRequestMessage(HttpMethod.Post, "reserve")
        {
            Content = JsonContent.Create(orderItem)
        };
        
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _http.SendAsync(request);
        
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