using DhaqaleysteApp.Models;

namespace DhaqaleysteApp.Pages;

public partial class ProductDetailsPage : ContentPage
{
    private readonly Product _product;

    public ProductDetailsPage(Product product)
    {
        InitializeComponent();

        _product = product;

        ProductNameLabel.Text = product.ProductName;
        PriceLabel.Text = $"${product.Price:0.00}";
        QuantityLabel.Text = product.Quantity > 0 ? $"In stock: {product.Quantity}" : "Out of stock";
        LocationLabel.Text = $"Location: {product.Location}";
        ContactLabel.Text = $"Contact: {product.Contact}";
        DescriptionLabel.Text = product.Description;

        // Images are web URLs, so they load through the cached URI image source.
        if (product.HasImage)
            ProductImage.Source = new UriImageSource { Uri = new Uri(product.ImageUrl), CachingEnabled = true, CacheValidity = TimeSpan.FromDays(7) };
        else
            ProductImage.IsVisible = false;

        PlayVideoButton.IsVisible = product.HasVideo;
    }

    private async void PlayVideo_Clicked(object sender, EventArgs e)
    {
        try
        {
            await Launcher.Default.OpenAsync(new Uri(_product.VideoUrl));
        }
        catch (Exception)
        {
            await DisplayAlert("Video", "The video could not be opened on this device.", "OK");
        }
    }
}
