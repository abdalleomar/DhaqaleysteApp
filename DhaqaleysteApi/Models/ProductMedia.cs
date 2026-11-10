namespace DhaqaleysteApi.Models
{
    public static class MediaKinds
    {
        public const string Image = "Image";
        public const string Video = "Video";

        public static bool IsValid(string kind) => kind is Image or Video;
    }

    /// <summary>An image or video of a product. The first image (lowest SortOrder) is the main photo.</summary>
    public class ProductMedia
    {
        public int ProductMediaId { get; set; }

        public int ProductId { get; set; }

        public Product? Product { get; set; }

        /// <summary><see cref="MediaKinds.Image"/> or <see cref="MediaKinds.Video"/>.</summary>
        public string Kind { get; set; } = MediaKinds.Image;

        /// <summary>Server-relative URL, e.g. /media/images/abc.jpg.</summary>
        public string Url { get; set; } = "";

        public int SortOrder { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
