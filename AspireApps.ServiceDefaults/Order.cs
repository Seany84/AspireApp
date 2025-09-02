namespace Microsoft.Extensions.Hosting;

public class Order
{
    public int Id { get; set; }
    public string Sku { get; set; }
    public decimal Price { get; set; }
}