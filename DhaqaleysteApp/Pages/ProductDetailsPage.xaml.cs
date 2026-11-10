using DhaqaleysteApp.Models;
using DhaqaleysteApp.Services;

namespace DhaqaleysteApp.Pages;

public partial class ProductDetailsPage : ContentPage
{
    private readonly ApiService _api = ServiceHelper.Get<ApiService>();
    private Product _product;
    private string _phone = "";
    private string _whatsApp = "";
    private bool _loaded;

    /// <summary>Opens with the list's summary right away, then loads the full details.</summary>
    public ProductDetailsPage(Product summary)
    {
        InitializeComponent();
        _product = summary;
        Show(summary);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loaded)
            return;

        var result = await _api.GetProductAsync(_product.ProductId);
        Busy.IsVisible = Busy.IsRunning = false;

        if (!result.Success)
        {
            ErrorLabel.Text = result.Error;
            ErrorLabel.IsVisible = true;
            return;
        }

        _loaded = true;
        _product = result.Data!;
        Show(_product);
        _ = _api.RecordViewAsync(_product.ProductId);

        await SetUpContactAsync();
    }

    private void Show(Product product)
    {
        CategoryLabel.Text = product.CategoryName;
        CategoryLabel.IsVisible = product.CategoryName.Length > 0;
        ProductNameLabel.Text = product.ProductName;
        PriceLabel.Text = product.PriceText;
        LocationLabel.Text = string.IsNullOrWhiteSpace(product.Location) ? "" : $"📍 {product.Location}";

        StockLabel.Text = product.StockText;
        StockLabel.TextColor = product.InStock ? Color.FromArgb("#047857") : Color.FromArgb("#B91C1C");
        StockBadge.BackgroundColor = product.InStock ? Color.FromArgb("#D1FAE5") : Color.FromArgb("#FEE2E2");

        var description = string.IsNullOrWhiteSpace(product.Description) ? product.ShortDescription : product.Description;
        DescriptionLabel.Text = description;
        DescriptionTitle.IsVisible = description.Length > 0;

        var images = product.Images.ToList();
        if (images.Count == 0 && product.HasImage)
            images.Add(new ProductMediaItem { DisplayUrl = product.ImageUrl });
        Gallery.ItemsSource = images;
        GalleryIndicator.IsVisible = images.Count > 1;

        VideoButtons.Children.Clear();
        var videos = product.Videos.ToList();
        for (var i = 0; i < videos.Count; i++)
        {
            var video = videos[i];
            var button = new Button { Text = videos.Count == 1 ? "▶ Play video" : $"▶ Play video {i + 1}" };
            button.Clicked += async (_, _) => await OpenAsync(video.DisplayUrl, "The video could not be opened on this device.");
            VideoButtons.Children.Add(button);
        }
        VideosSection.IsVisible = videos.Count > 0;
    }

    private async Task SetUpContactAsync()
    {
        var company = await _api.GetCachedCompanyAsync();

        // A product can override the company number (for example a branch).
        _phone = Digits(string.IsNullOrWhiteSpace(_product.Contact) ? company?.Phone : _product.Contact);
        _whatsApp = Digits(string.IsNullOrWhiteSpace(_product.Contact) ? company?.WhatsApp : _product.Contact);

        CallButton.IsVisible = _phone.Length > 0;
        WhatsAppButton.IsVisible = _whatsApp.Length > 0;
        Grid.SetColumnSpan(CallButton, WhatsAppButton.IsVisible ? 1 : 2);
        if (!CallButton.IsVisible)
        {
            Grid.SetColumn(WhatsAppButton, 0);
            Grid.SetColumnSpan(WhatsAppButton, 2);
        }
        ContactBar.IsVisible = CallButton.IsVisible || WhatsAppButton.IsVisible;
    }

    private static string Digits(string? number)
    {
        if (string.IsNullOrWhiteSpace(number))
            return "";
        var digits = new string(number.Where(char.IsDigit).ToArray());
        return number.TrimStart().StartsWith('+') ? "+" + digits : digits;
    }

    private async void Call_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (PhoneDialer.Default.IsSupported)
                PhoneDialer.Default.Open(_phone);
            else
                await DisplayAlert("Call", _phone, "OK");
        }
        catch (Exception)
        {
            await DisplayAlert("Call", $"Please call {_phone}", "OK");
        }
    }

    private async void WhatsApp_Clicked(object sender, EventArgs e)
    {
        var message = $"Hello, I am interested in \"{_product.ProductName}\" ({_product.PriceText}).";
        var url = $"https://wa.me/{_whatsApp.TrimStart('+')}?text={Uri.EscapeDataString(message)}";
        await OpenAsync(url, "WhatsApp could not be opened on this device.");
    }

    private async void Share_Clicked(object? sender, EventArgs e)
    {
        var lines = new List<string> { _product.ProductName, _product.PriceText };
        if (!string.IsNullOrWhiteSpace(_product.ShortDescription))
            lines.Add(_product.ShortDescription);
        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = _product.ProductName,
            Text = string.Join("\n", lines),
            Uri = _product.HasImage ? _product.ImageUrl : null
        });
    }

    private async Task OpenAsync(string url, string failureMessage)
    {
        try
        {
            if (!await Launcher.Default.OpenAsync(new Uri(url)))
                await DisplayAlert("Not available", failureMessage, "OK");
        }
        catch (Exception)
        {
            await DisplayAlert("Not available", failureMessage, "OK");
        }
    }
}
