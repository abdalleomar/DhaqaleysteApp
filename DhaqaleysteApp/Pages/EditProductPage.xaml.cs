using DhaqaleysteApp.Models;
using DhaqaleysteApp.Services;

namespace DhaqaleysteApp.Pages;

public partial class EditProductPage : ContentPage
{
    private readonly Product _product;

    public EditProductPage(Product product)
    {
        InitializeComponent();

        _product = product;

        ProductNameEntry.Text = product.ProductName;
        DescriptionEditor.Text = product.Description;
        PriceEntry.Text = product.Price.ToString();
        QuantityEntry.Text = product.Quantity.ToString();
        ContactEntry.Text = product.Contact;
        LocationEntry.Text = product.Location;
        CategoryEntry.Text = product.Category;
    }

    private async void UpdateProduct_Clicked(object sender, EventArgs e)
    {
        _product.ProductName = ProductNameEntry.Text ?? "";
        _product.Description = DescriptionEditor.Text ?? "";
        _product.Price = decimal.Parse(PriceEntry.Text ?? "0");
        _product.Quantity = int.Parse(QuantityEntry.Text ?? "0");
        _product.Contact = ContactEntry.Text ?? "";
        _product.Location = LocationEntry.Text ?? "";
        _product.Category = CategoryEntry.Text ?? "";

        var api = new ApiService();

        bool success =
            await api.UpdateProductAsync(_product);

        if (success)
        {
            await DisplayAlert(
                "Success",
                "Product Updated",
                "OK");

            await Navigation.PopAsync();
        }
    }
}