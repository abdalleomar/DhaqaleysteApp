namespace DhaqaleysteApi.Models
{
    public class Product
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; } = "";

        public string Description { get; set; } = "";

        public decimal Price { get; set; }

        public int Quantity { get; set; }

        /// <summary>Optional per-product contact. When empty the app uses the company phone/WhatsApp.</summary>
        public string Contact { get; set; } = "";

        public string Location { get; set; } = "";

        public int? CategoryId { get; set; }

        public Category? Category { get; set; }

        /// <summary>False when staff mark the product as sold out / unavailable.</summary>
        public bool IsAvailable { get; set; } = true;

        /// <summary>Shown in the "Featured" section of the home screen.</summary>
        public bool IsFeatured { get; set; }

        public int ViewCount { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAtUtc { get; set; }

        public int? CreatedByUserId { get; set; }

        public int? UpdatedByUserId { get; set; }

        /// <summary>Deleted products are hidden everywhere but kept in the database.</summary>
        public bool IsDeleted { get; set; }

        public DateTime? DeletedAtUtc { get; set; }

        public List<ProductMedia> Media { get; set; } = new();

        /// <summary>In stock means available to buy now.</summary>
        public bool InStock => IsAvailable && Quantity > 0;
    }
}
