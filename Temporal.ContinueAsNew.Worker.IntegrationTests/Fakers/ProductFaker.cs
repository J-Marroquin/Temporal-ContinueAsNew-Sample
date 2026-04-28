using Bogus;
using Temporal.ContinueAsNew.Worker.Models;

namespace Temporal.ContinueAsNew.Worker.IntegrationTests.Fakers;

public static class ProductFaker
{
    private static readonly Faker<Product> Faker = new Faker<Product>()
        .RuleFor(p => p.Id, f => f.Commerce.Product())
        .RuleFor(p => p.Title, f => f.Commerce.ProductName())
        .RuleFor(p => p.OnStock, f => f.Random.Int(0, 100));

    public static Product Generate(string itemId)
    {
        var product = Faker.Generate();
        product.Id = itemId; // override to match request
        return product;
    }
}