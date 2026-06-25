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
        PriceLabel.Text = $"Price: ${product.Price}";
        QuantityLabel.Text = $"Qty: {product.Quantity}";
        LocationLabel.Text = $"Location: {product.Location}";
        ContactLabel.Text = $"Contact: {product.Contact}";
        DescriptionLabel.Text = product.Description;

        if (!string.IsNullOrEmpty(product.ImagePath))
        {
            ProductImage.Source = ImageSource.FromFile(product.ImagePath);
        }

        if (!string.IsNullOrEmpty(product.VideoPath))
        {
            PlayVideoButton.IsVisible = true;
        }
    }

    private async void PlayVideo_Clicked(object sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(_product.VideoPath))
        {
            await Launcher.Default.OpenAsync(_product.VideoPath);
        }
    }
}
