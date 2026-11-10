using System.ComponentModel.DataAnnotations;

namespace DhaqaleysteApi.Dtos
{
    public class CategoryDto
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = "";
        public string NameSo { get; set; } = "";
        public string Icon { get; set; } = "";
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        /// <summary>Number of products in this category (not deleted).</summary>
        public int ProductCount { get; set; }
    }

    public class CategoryRequest
    {
        [Required, StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = "";

        [StringLength(100)]
        public string? NameSo { get; set; }

        [StringLength(50)]
        public string? Icon { get; set; }

        [Range(0, 10_000)]
        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
