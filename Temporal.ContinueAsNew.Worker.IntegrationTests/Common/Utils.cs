using Bogus;

namespace Temporal.ContinueAsNew.Worker.IntegrationTests.Common;

public static class Utils
{
    public static string CreateTestCsv(string fileName, int rows = 5)
    {
        var path = Path.Combine(AppContext.BaseDirectory, fileName);

        var lines = new List<string>
        {
            "ItemId,Quantity"
        };

        for (int i = 1; i <= rows; i++)
        {
            lines.Add($"{i},{1}");
        }

        File.WriteAllLines(path, lines);

        return path;
    }
    
    public static string GetRandomHexHash()
    {
        var hex = new Faker().Random.Hexadecimal(64);
        hex = hex.StartsWith("0x",StringComparison.OrdinalIgnoreCase) ? hex[2..] : hex;

        return hex.ToLowerInvariant();
    }
}