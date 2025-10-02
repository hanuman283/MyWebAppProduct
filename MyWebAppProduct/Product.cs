namespace MyWebAppProduct
{
    public class Product
    {
        public int Id { get; set; } // Unique identifier
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
    }
}
