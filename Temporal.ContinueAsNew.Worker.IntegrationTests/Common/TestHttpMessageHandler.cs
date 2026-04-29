using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using Temporal.ContinueAsNew.Worker.IntegrationTests.Fakers;
using Temporal.ContinueAsNew.Worker.Models;
using Temporal.ContinueAsNew.Worker.Models.DTOs;

namespace Temporal.ContinueAsNew.Worker.IntegrationTests.Common;

public class TestHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage>? _handler;
    private readonly ConcurrentDictionary<string, int> _transientCounters = new();
    private static readonly ThreadLocal<Random> Random = new(() => new Random());
    
    private static readonly string[] BusinessErrors =
    {
        "Insufficient quantity available",
        "Item is discontinued",
        "Invalid quantity requested",
        "Reservation limit exceeded",
        "Item not found in catalog",
        "Item is restricted for this order",
        "Duplicate reservation detected"
    };
    
    public TestHttpMessageHandler()
    {
    }

    public TestHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _handler = handler;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // If custom handler provided, use it
        if (_handler != null)
            return _handler(request);

        // Default behavior
        return await HandleDefault(request);
    }

    private async Task<HttpResponseMessage> HandleDefault(HttpRequestMessage request)
    {
        var path = request.RequestUri!.AbsolutePath;

        // Handle product lookup 
        if (path.Contains("products"))
            return HandleProducts(request);

        // Handle reserve endpoint (async)
        if (path.Contains("reserve"))
            return await HandleReserve(request);

        // Default fallback
        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    private HttpResponseMessage HandleProducts(HttpRequestMessage request)
    {
        var query = QueryHelpers.ParseQuery(request.RequestUri!.Query);
        var itemId = query["itemid"].ToString();
        
        var product = ProductFaker.Generate(itemId);
        
        product.Id = string.IsNullOrWhiteSpace(itemId) ? product.Id : itemId;
        
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(product)
        };
    }

    private async Task<HttpResponseMessage> HandleReserve(HttpRequestMessage request)
    {
        var itemId = await ExtractItemIdAsync(request);

        var counter = _transientCounters.AddOrUpdate(
            itemId,
            1,
            (_, current) => current + 1);

        // 1. Simulate 2 transient errors PER ITEM
        if (counter < 2)
        {
            _transientCounters[itemId]++;
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable); // 503
        }
        
        // Reset counter after burst
        _transientCounters[itemId] = 0;
        
        
        // 2. Occasionally non-transient error
        if (Random.Value!.Next(0, 10) == 0)
        {
            var message = BusinessErrors[Random.Value!.Next(BusinessErrors.Length)];
            
            return new HttpResponseMessage(HttpStatusCode.BadRequest) // 400
            {
                Content = JsonContent.Create(new ReserveItemResponseDto
                {
                    IsSuccess = false,
                    ErrorMessage = message,
                    ErrorCode = 100
                })
            };
        }
            

        // 3. Success
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new ReserveItemResponseDto
            {
                IsSuccess = true
            })
        };
    }

    private async Task<string> ExtractItemIdAsync(HttpRequestMessage request)
    {
        var json = await request.Content!.ReadAsStringAsync();

        var body = System.Text.Json.JsonSerializer.Deserialize<OrderItem>(json);
        
        request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        return body?.ItemId ?? "unknown";
        
    }
}