namespace Temporal.ContinueAsNew.Worker.Models;

public class Product
{
    public string Id { get; set; }
    public string Title { get; set; } = null!;
    public int OnStock { get; set; }
}