using System.ComponentModel.DataAnnotations;
using DhaqaleysteApi.Models;

namespace DhaqaleysteApi.Dtos
{
    /// <summary>Public product shape. Field names match the existing mobile app.</summary>
    public class ProductDto
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

        public static ProductDto From(Product p) => new()
        {
            ProductId = p.ProductId,
            ProductName = p.ProductName,
            Description = p.Description,
            Price = p.Price,
            Quantity = p.Quantity,
            Contact = p.Contact,
            Location = p.Location,
            Category = p.Category,
            ImagePath = p.ImagePath,
            VideoPath = p.VideoPath
        };
    }

    /// <summary>Used for both create and update. The id always comes from the route, never the body.</summary>
    public class ProductRequest
    {
        [Required, StringLength(200, MinimumLength = 2)]
        public string ProductName { get; set; } = "";

        [StringLength(4000)]
        public string? Description { get; set; }

        [Range(typeof(decimal), "0", "100000000")]
        public decimal Price { get; set; }

        [Range(0, 1_000_000)]
        public int Quantity { get; set; }

        [StringLength(100)]
        public string? Contact { get; set; }

        [StringLength(200)]
        public string? Location { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }

        [StringLength(500)]
        public string? ImagePath { get; set; }

        [StringLength(500)]
        public string? VideoPath { get; set; }

        public void ApplyTo(Product p)
        {
            p.ProductName = ProductName.Trim();
            p.Description = Description?.Trim() ?? "";
            p.Price = Price;
            p.Quantity = Quantity;
            p.Contact = Contact?.Trim() ?? "";
            p.Location = Location?.Trim() ?? "";
            p.Category = Category?.Trim() ?? "";
            p.ImagePath = ImagePath?.Trim() ?? "";
            p.VideoPath = VideoPath?.Trim() ?? "";
        }
    }

    public class UploadResult
    {
        /// <summary>Server-relative URL, e.g. /media/images/abc.jpg. Store this on the product.</summary>
        public string Url { get; set; } = "";
    }
}
