using DhaqaleysteApp.Models;
using DhaqaleysteApp.Services;

namespace DhaqaleysteApp.Pages;

public partial class AddProductPage : ContentPage
{
    public AddProductPage()
    {
        InitializeComponent();
    }
    private string _selectedImagePath = "";
    private string _selectedVideoPath = "";
    private async void SaveProduct_Clicked(object sender, EventArgs e)
    {
        var api = new ApiService();

        string imageUrl = "";
        string videoUrl = "";

        if (!string.IsNullOrEmpty(_selectedImagePath))
            imageUrl = await api.UploadImageAsync(_selectedImagePath);

        if (!string.IsNullOrEmpty(_selectedVideoPath))
            videoUrl = await api.UploadVideoAsync(_selectedVideoPath);

        var product = new Product
        {
            ProductName = ProductNameEntry.Text ?? "",
            Description = DescriptionEditor.Text ?? "",
            Price = decimal.TryParse(PriceEntry.Text, out var price) ? price : 0,
            Quantity = int.TryParse(QuantityEntry.Text, out var qty) ? qty : 0,
            Contact = ContactEntry.Text ?? "",
            Location = LocationEntry.Text ?? "",
            Category = CategoryEntry.Text ?? "",
            ImagePath = imageUrl,
            VideoPath = videoUrl
        };

        bool saved = await api.AddProductAsync(product);

        if (saved)
        {
            await DisplayAlert("Success", "Product Saved to Server", "OK");
            await Navigation.PopToRootAsync();
        }
        else
        {
            await DisplayAlert("Error", "Product not saved", "OK");
        }
    }
    private async void UploadImage_Clicked(object sender, EventArgs e)
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select Product Image"
            });

            if (file != null)
            {
                _selectedImagePath = file.FullPath;
                PreviewImage.Source = ImageSource.FromFile(file.FullPath);

                await DisplayAlert(
                    "Image Selected",
                    file.FileName,
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }
    private async void UploadVideo_Clicked(object sender, EventArgs e)
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select Product Video"
            });

            if (file == null)
                return;

            var fileInfo = new FileInfo(file.FullPath);
            long maxSize = 50 * 1024 * 1024; // 50MB

            if (fileInfo.Length > maxSize)
            {
                await DisplayAlert("Too Large", "Video maximum size is 50MB.", "OK");
                return;
            }

            _selectedVideoPath = file.FullPath;

            await DisplayAlert("Video Selected", file.FileName, "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }
}