
namespace DhaqaleysteApi.Models
{
    public class Product
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; } = "";

        public string Description { get; set; } = "";

        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public string Contact { get; set; } = "";

        public string Location { get; set; } = "";

        public string Category { get; set; } = "";

        public string ImagePath { get; set; } = "";

        public string VideoPath { get; set; } = "";
    }
}