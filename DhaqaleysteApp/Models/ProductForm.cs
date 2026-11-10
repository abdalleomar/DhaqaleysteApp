namespace DhaqaleysteApp.Models;

/// <summary>Body for creating or updating a product (matches the API's ProductRequest).</summary>
public class ProductForm
{
    public string ProductName { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string Contact { get; set; } = "";
    public string Location { get; set; } = "";
    public int? CategoryId { get; set; }
    public bool IsAvailable { get; set; } = true;
    public bool IsFeatured { get; set; }
    public List<MediaReference> Media { get; set; } = new();
}

public class MediaReference
{
    public string Kind { get; set; } = ProductMediaItem.ImageKind;
    public string Url { get; set; } = "";
}
