namespace DhaqaleysteApi.Models
{
    public class Category
    {
        public int CategoryId { get; set; }

        /// <summary>English name (required, unique).</summary>
        public string Name { get; set; } = "";

        /// <summary>Somali name. The app falls back to <see cref="Name"/> when empty.</summary>
        public string NameSo { get; set; } = "";

        /// <summary>Optional icon key or emoji shown on category chips.</summary>
        public string Icon { get; set; } = "";

        public int SortOrder { get; set; }

        /// <summary>Inactive categories are hidden from customers.</summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAtUtc { get; set; }

        public List<Product> Products { get; set; } = new();
    }
}
