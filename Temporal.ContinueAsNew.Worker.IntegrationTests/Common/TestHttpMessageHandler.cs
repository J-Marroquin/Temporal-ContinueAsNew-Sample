using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using Temporal.ContinueAsNew.Worker.IntegrationTests.Fakers;

namespace Temporal.ContinueAsNew.Worker.IntegrationTests.Common;

public class TestHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage>? _handler;
    
    public TestHttpMessageHandler() { }
    
    public TestHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _handler = handler;
    }
    
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // If custom handler provided, use it
        if (_handler != null)
            return Task.FromResult(_handler(request));

        // Default behavior
        return Task.FromResult(HandleDefault(request));
    }
    
    private HttpResponseMessage HandleDefault(HttpRequestMessage request)
    {
        var path = request.RequestUri!.AbsolutePath;

        // Handle product lookup
        if (path.Contains("products"))
        {
            var query = QueryHelpers.ParseQuery(request.RequestUri.Query);
            var itemId = query["itemid"].ToString();

            var product = ProductFaker.Generate(itemId);
            product.Id = string.IsNullOrWhiteSpace(itemId) ? product.Id : itemId;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(product)
            };
        }

        // Handle reserve endpoint
        if (path.Contains("reserve"))
        {
            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        // Default fallback
        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }

}