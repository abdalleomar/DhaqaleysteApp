using System.Collections.ObjectModel;
using System.Globalization;
using DhaqaleysteApp.Models;
using DhaqaleysteApp.Services;

namespace DhaqaleysteApp.Pages;

/// <summary>Adds a new product, or edits an existing one when a product is passed in.</summary>
public partial class ProductFormPage : ContentPage
{
    private const int MaxImages = 10;
    private const int MaxVideos = 3;
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
    private readonly List<EditableMedia> _videos = new();
    private List<Category> _categories = new();
    private bool _loaded;

    public ObservableCollection<EditableMedia> Images { get; } = new();

    public ProductFormPage(Product? product = null)
    {
        InitializeComponent();
        BindingContext = this;
        _original = product;

        if (product != null)
        {
            Title = HeadingLabel.Text = "Edit Product";
            SaveButton.Text = "Update Product";
        }
        else
        {
            Title = "Add Product";
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loaded)
            return;

        SetBusy(true, "Loading…");
        try
        {
            var categories = await _api.GetCategoriesAsync(includeHidden: true);
            if (categories.Success)
            {
                _categories = categories.Data!;
                CategoryPicker.ItemsSource = _categories;
            }

            if (_original != null)
            {
                // Lists only carry a summary; the form needs the full product with its media.
                var details = await _api.GetProductAsync(_original.ProductId);
                if (!details.Success)
                {
                    ShowError(details.Error);
                    SaveButton.IsEnabled = false;
                    return;
                }
                Fill(details.Data!);
            }

            _loaded = true;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void Fill(Product product)
    {
        ProductNameEntry.Text = product.ProductName;
        DescriptionEditor.Text = product.Description;
        PriceEntry.Text = product.Price.ToString("0.##", CultureInfo.InvariantCulture);
        QuantityEntry.Text = product.Quantity.ToString(CultureInfo.InvariantCulture);
        ContactEntry.Text = product.Contact;
        LocationEntry.Text = product.Location;
        AvailableSwitch.IsToggled = product.IsAvailable;
        FeaturedSwitch.IsToggled = product.IsFeatured;
        CategoryPicker.SelectedItem = _categories.FirstOrDefault(c => c.CategoryId == product.CategoryId);

        foreach (var image in product.Images)
            Images.Add(EditableMedia.FromServer(image));
        foreach (var video in product.Videos)
            _videos.Add(EditableMedia.FromServer(video));
        RefreshMedia();
    }

    // ---------- Images ----------

    private async void AddImages_Clicked(object sender, EventArgs e)
    {
        var room = MaxImages - Images.Count;
        if (room <= 0)
        {
            await DisplayAlert("Images", $"A product can have at most {MaxImages} images.", "OK");
            return;
        }

        try
        {
            var files = (await FilePicker.Default.PickMultipleAsync(new PickOptions
            {
                PickerTitle = "Select product images",
                FileTypes = FilePickerFileType.Images
            }))?.Where(f => f != null).ToList() ?? new List<FileResult>();

            var tooLarge = files.Where(f => new FileInfo(f.FullPath).Length > MaxImageBytes).ToList();
            foreach (var file in files.Except(tooLarge).Take(room))
                Images.Add(EditableMedia.FromLocal(ProductMediaItem.ImageKind, file));

            if (tooLarge.Count > 0)
                await DisplayAlert("Some images skipped", $"{tooLarge.Count} image(s) are larger than 5 MB.", "OK");
            if (files.Count - tooLarge.Count > room)
                await DisplayAlert("Some images skipped", $"Only {MaxImages} images are allowed.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Could not open images", ex.Message, "OK");
        }
        RefreshMedia();
    }

    private async void Image_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is not BindableObject { BindingContext: EditableMedia image })
            return;

        var buttons = image.IsMain ? Array.Empty<string>() : new[] { "Make main photo" };
        var choice = await DisplayActionSheet("Image", "Cancel", "Remove", buttons);
        if (choice == "Remove")
            Images.Remove(image);
        else if (choice == "Make main photo")
            Images.Move(Images.IndexOf(image), 0);
        RefreshMedia();
    }

    // ---------- Videos ----------

    private async void AddVideo_Clicked(object sender, EventArgs e)
    {
        if (_videos.Count >= MaxVideos)
        {
            await DisplayAlert("Videos", $"A product can have at most {MaxVideos} videos.", "OK");
            return;
        }

        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Select product video", FileTypes = VideoTypes });
            if (file == null)
                return;

            if (new FileInfo(file.FullPath).Length > MaxVideoBytes)
            {
                await DisplayAlert("File too large", "The maximum video size is 50 MB.", "OK");
                return;
            }
            _videos.Add(EditableMedia.FromLocal(ProductMediaItem.VideoKind, file));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Could not open video", ex.Message, "OK");
        }
        RefreshMedia();
    }

    private void RefreshMedia()
    {
        for (var i = 0; i < Images.Count; i++)
            Images[i].IsMain = i == 0;
        ImagesTitle.Text = $"Images ({Images.Count}/{MaxImages})";
        VideosTitle.Text = $"Videos ({_videos.Count}/{MaxVideos})";

        VideosList.Children.Clear();
        foreach (var video in _videos)
        {
            var row = new Grid { ColumnDefinitions = new ColumnDefinitionCollection(new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)) };
            row.Add(new Label { Text = "🎬 " + video.Label, VerticalOptions = LayoutOptions.Center, LineBreakMode = LineBreakMode.MiddleTruncation });
            var remove = new Button { Text = "Remove", BackgroundColor = Colors.Transparent, TextColor = Color.FromArgb("#B91C1C") };
            remove.Clicked += (_, _) => { _videos.Remove(video); RefreshMedia(); };
            row.Add(remove, 1);
            VideosList.Children.Add(row);
        }
    }

    // ---------- Save ----------

    private async void Save_Clicked(object sender, EventArgs e)
    {
        if (Busy.IsRunning)
            return; // prevents duplicate products from double taps

        var form = BuildForm(out var error);
        if (form == null)
        {
            ShowError(error);
            return;
        }

        try
        {
            // Upload new files one by one. Uploaded ones keep their server URL, so a retry skips them.
            var pending = Images.Concat(_videos).Where(m => m.NeedsUpload).ToList();
            for (var i = 0; i < pending.Count; i++)
            {
                SetBusy(true, $"Uploading {i + 1} of {pending.Count}…");
                var media = pending[i];
                var upload = media.IsImage
                    ? await _api.UploadImageAsync(media.LocalPath)
                    : await _api.UploadVideoAsync(media.LocalPath);
                if (!upload.Success)
                {
                    ShowError($"\"{media.Label}\" could not be uploaded: {upload.Error}");
                    return;
                }
                media.ServerUrl = upload.Data!;
            }

            form.Media = Images.Concat(_videos)
                .Select(m => new MediaReference { Kind = m.Kind, Url = m.ServerUrl })
                .ToList();

            SetBusy(true, "Saving…");
            var result = _original == null
                ? await _api.AddProductAsync(form)
                : await _api.UpdateProductAsync(_original.ProductId, form);

            if (!result.Success)
            {
                ShowError(result.Error);
                return;
            }

            _original?.CopyFrom(result.Data!);
            await DisplayAlert("Saved", _original == null ? "Product added." : "Product updated.", "OK");
            await Navigation.PopAsync();
        }
        finally
        {
            SetBusy(false);
        }
    }

    /// <summary>Reads and validates the form. Returns null with a message when something is wrong.</summary>
    private ProductForm? BuildForm(out string error)
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

        return new ProductForm
        {
            ProductName = name,
            Description = DescriptionEditor.Text?.Trim() ?? "",
            Price = Math.Round(price, 2),
            Quantity = quantity,
            Contact = ContactEntry.Text?.Trim() ?? "",
            Location = LocationEntry.Text?.Trim() ?? "",
            CategoryId = (CategoryPicker.SelectedItem as Category)?.CategoryId,
            IsAvailable = AvailableSwitch.IsToggled,
            IsFeatured = FeaturedSwitch.IsToggled
        };
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

    private void SetBusy(bool busy, string message = "")
    {
        Busy.IsVisible = Busy.IsRunning = busy;
        BusyLabel.Text = busy ? message : "";
        SaveButton.IsEnabled = !busy;
        if (busy)
            ErrorLabel.IsVisible = false;
    }
}

/// <summary>An image or video in the form: either already on the server, or a local file to upload.</summary>
public class EditableMedia : BindableModel
{
    private bool _isMain;

    public string Kind { get; init; } = ProductMediaItem.ImageKind;
    public string ServerUrl { get; set; } = "";
    public string LocalPath { get; init; } = "";
    public string Label { get; init; } = "";
    public ImageSource? Preview { get; init; }

    public bool IsImage => Kind == ProductMediaItem.ImageKind;
    public bool NeedsUpload => ServerUrl.Length == 0;

    public bool IsMain
    {
        get => _isMain;
        set => Set(ref _isMain, value);
    }

    public static EditableMedia FromServer(ProductMediaItem item) => new()
    {
        Kind = item.Kind,
        ServerUrl = item.Url,
        Label = Path.GetFileName(item.Url),
        Preview = item.IsImage && Uri.TryCreate(item.DisplayUrl, UriKind.Absolute, out var uri) ? new UriImageSource { Uri = uri } : null
    };

    public static EditableMedia FromLocal(string kind, FileResult file) => new()
    {
        Kind = kind,
        LocalPath = file.FullPath,
        Label = file.FileName,
        Preview = kind == ProductMediaItem.ImageKind ? ImageSource.FromFile(file.FullPath) : null
    };
}
