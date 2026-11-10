using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DhaqaleysteApp.Models;
using DhaqaleysteApp.Services;
using DhaqaleysteApp.Services.Localization;

namespace DhaqaleysteApp.ViewModels;

public partial class ProductDetailsViewModel : BaseViewModel
{
    private readonly ApiService _api;
    private readonly LocalStore _store;
    private bool _loaded;
    private string _phone = "";
    private string _whatsApp = "";

    public ProductDetailsViewModel(ApiService api, LocalStore store)
    {
        _api = api;
        _store = store;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description), nameof(HasDescription))]
    private Product _product = new();

    public ObservableCollection<ProductMediaItem> Images { get; } = new();
    public ObservableCollection<VideoItem> Videos { get; } = new();

    [ObservableProperty] private bool _hasManyImages;
    [ObservableProperty] private bool _hasVideos;
    [ObservableProperty] private bool _canCall;
    [ObservableProperty] private bool _canWhatsApp;
    [ObservableProperty] private bool _hasContact;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FavoriteIcon), nameof(FavoriteText))]
    private bool _isFavorite;

    public string FavoriteIcon => IsFavorite ? "" : "";
    public string FavoriteText => IsFavorite ? Loc.T("RemoveFavorite") : Loc.T("AddFavorite");

    public string Description => string.IsNullOrWhiteSpace(Product.Description) ? Product.ShortDescription : Product.Description;
    public bool HasDescription => Description.Length > 0;

    /// <summary>Shows the list's summary immediately; the full product loads when the page appears.</summary>
    public void Initialize(Product summary)
    {
        Product = summary;
        ShowImages(summary);
    }

    public async Task OnAppearingAsync()
    {
        IsFavorite = await _store.IsFavoriteAsync(Product.ProductId);
        if (_loaded)
            return;

        IsLoading = true;
        var result = await _api.GetProductAsync(Product.ProductId);
        IsLoading = false;

        if (!result.Success)
        {
            // Keep showing the summary we have; explain why details are missing.
            SetError(result.IsNetworkError ? result.Error : Loc.T("ProductNotFound"), result.IsNetworkError);
            if (!result.IsNetworkError)
                await _store.RemoveRecentAsync(new[] { Product.ProductId });
            return;
        }

        ClearError();
        _loaded = true;
        Product = result.Data!;
        ShowImages(Product);
        Videos.Clear();
        var videos = Product.Videos.ToList();
        for (var i = 0; i < videos.Count; i++)
            Videos.Add(new VideoItem(videos[i].DisplayUrl, videos.Count == 1 ? Loc.T("PlayVideo") : Loc.T("PlayVideoNumber", i + 1)));
        HasVideos = Videos.Count > 0;

        _ = _api.RecordViewAsync(Product.ProductId);
        await _store.AddRecentAsync(Product);
        await SetUpContactAsync();
    }

    private void ShowImages(Product product)
    {
        Images.Clear();
        foreach (var image in product.Images)
            Images.Add(image);
        if (Images.Count == 0 && product.HasImage)
            Images.Add(new ProductMediaItem { DisplayUrl = product.ImageUrl });
        HasManyImages = Images.Count > 1;
    }

    private async Task SetUpContactAsync()
    {
        var company = await _api.GetCachedCompanyAsync();
        // A product can carry its own number (for example a branch); otherwise use the company's.
        var own = Product.Contact;
        _phone = ContactLauncher.CleanNumber(string.IsNullOrWhiteSpace(own) ? company?.Phone : own);
        _whatsApp = ContactLauncher.CleanNumber(string.IsNullOrWhiteSpace(own) ? company?.WhatsApp : own);
        CanCall = _phone.Length > 0;
        CanWhatsApp = _whatsApp.Length > 0;
        HasContact = CanCall || CanWhatsApp;
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync()
    {
        IsFavorite = await _store.ToggleFavoriteAsync(Product);
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch (Exception) { }
    }

    [RelayCommand]
    private Task CallAsync() => ContactLauncher.CallAsync(_phone);

    [RelayCommand]
    private Task WhatsAppAsync() =>
        ContactLauncher.WhatsAppAsync(_whatsApp, Loc.T("WhatsAppMessage", Product.ProductName, Product.PriceText));

    [RelayCommand]
    private Task ShareAsync()
    {
        var lines = new List<string> { Product.ProductName, Product.PriceText };
        if (!string.IsNullOrWhiteSpace(Product.ShortDescription))
            lines.Add(Product.ShortDescription);
        return Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = Product.ProductName,
            Text = string.Join("\n", lines),
            Uri = Product.HasImage ? Product.ImageUrl : null
        });
    }

    [RelayCommand]
    private Task PlayVideoAsync(VideoItem video) => ContactLauncher.OpenUrlAsync(video.Url);
}

public record VideoItem(string Url, string Label);
