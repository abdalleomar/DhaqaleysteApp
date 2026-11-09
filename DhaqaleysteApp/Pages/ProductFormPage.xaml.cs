using System.Globalization;
using DhaqaleysteApp.Models;
using DhaqaleysteApp.Services;

namespace DhaqaleysteApp.Pages;

/// <summary>Adds a new product, or edits an existing one when a product is passed in.</summary>
public partial class ProductFormPage : ContentPage
{
    private const long MaxImageBytes = 5 * 1024 * 1024;
    private const long MaxVideoBytes = 50 * 1024 * 1024;

    private static readonly FilePickerFileType VideoTypes = new(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        [DevicePlatform.Android] = new[] { "video/mp4", "video/quicktime", "video/webm" },
        [DevicePlatform.iOS] = new[] { "public.movie" },
        [DevicePlatform.MacCatalyst] = new[] { "public.movie" },
        [DevicePlatform.WinUI] = new[] { ".mp4", ".mov", ".m4v", ".webm" }
    });

    private readonly ApiService _api = ServiceHelper.Get<ApiService>();
    private readonly Product? _original;

    private string _selectedImagePath = "";
    private string _selectedVideoPath = "";

    public ProductFormPage(Product? product = null)
    {
        InitializeComponent();
        _original = product;

        if (product == null)
        {
            Title = "Add Product";
            return;
        }

        Title = HeadingLabel.Text = "Edit Product";
        SaveButton.Text = "Update Product";

        ProductNameEntry.Text = product.ProductName;
        DescriptionEditor.Text = product.Description;
        PriceEntry.Text = product.Price.ToString("0.##", CultureInfo.InvariantCulture);
        QuantityEntry.Text = product.Quantity.ToString(CultureInfo.InvariantCulture);
        ContactEntry.Text = product.Contact;
        LocationEntry.Text = product.Location;
        CategoryEntry.Text = product.Category;

        if (product.HasImage)
            PreviewImage.Source = new UriImageSource { Uri = new Uri(product.ImageUrl) };
        if (product.HasVideo)
            VideoLabel.Text = "Current video kept unless you choose a new one";
    }

    private async void ChooseImage_Clicked(object sender, EventArgs e)
    {
        var file = await PickAsync(FilePickerFileType.Images, "Select product image", MaxImageBytes);
        if (file == null)
            return;

        _selectedImagePath = file.FullPath;
        PreviewImage.Source = ImageSource.FromFile(file.FullPath);
    }

    private async void ChooseVideo_Clicked(object sender, EventArgs e)
    {
        var file = await PickAsync(VideoTypes, "Select product video", MaxVideoBytes);
        if (file == null)
            return;

        _selectedVideoPath = file.FullPath;
        VideoLabel.Text = file.FileName;
    }

    private async Task<FileResult?> PickAsync(FilePickerFileType types, string title, long maxBytes)
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = title, FileTypes = types });
            if (file == null)
                return null;

            if (new FileInfo(file.FullPath).Length > maxBytes)
            {
                await DisplayAlert("File too large", $"The maximum size is {maxBytes / (1024 * 1024)} MB.", "OK");
                return null;
            }
            return file;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Could not open file", ex.Message, "OK");
            return null;
        }
    }

    private async void Save_Clicked(object sender, EventArgs e)
    {
        if (Busy.IsRunning)
            return; // prevents duplicate products from double taps

        var product = BuildFromForm(out var error);
        if (product == null)
        {
            ShowError(error);
            return;
        }

        SetBusy(true);
        try
        {
            if (_selectedImagePath.Length > 0)
            {
                var upload = await _api.UploadImageAsync(_selectedImagePath);
                if (!upload.Success)
                {
                    ShowError($"Image upload failed: {upload.Error}");
                    return;
                }
                product.ImagePath = upload.Data!;
                _selectedImagePath = "";
            }

            if (_selectedVideoPath.Length > 0)
            {
                var upload = await _api.UploadVideoAsync(_selectedVideoPath);
                if (!upload.Success)
                {
                    ShowError($"Video upload failed: {upload.Error}");
                    return;
                }
                product.VideoPath = upload.Data!;
                _selectedVideoPath = "";
            }

            var result = _original == null
                ? await _api.AddProductAsync(product)
                : await _api.UpdateProductAsync(product);

            if (!result.Success)
            {
                ShowError(result.Error);
                return;
            }

            if (_original != null)
            {
                product.ImageUrl = _api.ResolveMediaUrl(product.ImagePath);
                product.VideoUrl = _api.ResolveMediaUrl(product.VideoPath);
                _original.CopyFrom(product);
            }

            await DisplayAlert("Saved", _original == null ? "Product added." : "Product updated.", "OK");
            await Navigation.PopAsync();
        }
        finally
        {
            SetBusy(false);
        }
    }

    /// <summary>Reads and validates the form. Returns null with a message when something is wrong.</summary>
    private Product? BuildFromForm(out string error)
    {
        error = "";
        var name = ProductNameEntry.Text?.Trim() ?? "";
        if (name.Length < 2)
        {
            error = "Enter a product name (at least 2 characters).";
            return null;
        }

        if (!TryParseDecimal(PriceEntry.Text, out var price) || price < 0)
        {
            error = "Enter a valid price, for example 12.50.";
            return null;
        }

        if (!int.TryParse(QuantityEntry.Text?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var quantity))
        {
            error = "Enter a valid quantity (a whole number, 0 or more).";
            return null;
        }

        var product = _original?.Clone() ?? new Product();
        product.ProductName = name;
        product.Description = DescriptionEditor.Text?.Trim() ?? "";
        product.Price = Math.Round(price, 2);
        product.Quantity = quantity;
        product.Contact = ContactEntry.Text?.Trim() ?? "";
        product.Location = LocationEntry.Text?.Trim() ?? "";
        product.Category = CategoryEntry.Text?.Trim() ?? "";
        return product;
    }

    /// <summary>Accepts both "12.50" and "12,50" regardless of the phone's language settings.</summary>
    private static bool TryParseDecimal(string? text, out decimal value)
    {
        var normalized = (text ?? "").Trim().Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    private void SetBusy(bool busy)
    {
        Busy.IsVisible = Busy.IsRunning = busy;
        SaveButton.IsEnabled = !busy;
        if (busy)
            ErrorLabel.IsVisible = false;
    }
}
