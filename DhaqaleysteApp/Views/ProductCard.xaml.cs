using DhaqaleysteApp.Models;

namespace DhaqaleysteApp.Views;

public partial class ProductCard : ContentView
{
    /// <summary>Hide the "Featured" badge where every card is featured (the Featured carousel).</summary>
    public static readonly BindableProperty ShowFeaturedBadgeProperty =
        BindableProperty.Create(nameof(ShowFeaturedBadge), typeof(bool), typeof(ProductCard), true,
            propertyChanged: (b, _, _) => ((ProductCard)b).UpdateBadge());

    private Product? _product;

    public ProductCard()
    {
        InitializeComponent();
        UpdateBadge();
    }

    public bool ShowFeaturedBadge
    {
        get => (bool)GetValue(ShowFeaturedBadgeProperty);
        set => SetValue(ShowFeaturedBadgeProperty, value);
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (_product != null)
            _product.PropertyChanged -= ProductChanged;
        _product = BindingContext as Product;
        if (_product != null)
            _product.PropertyChanged += ProductChanged;
        UpdateBadge();
    }

    private void ProductChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Product.IsFeatured))
            UpdateBadge();
    }

    private void UpdateBadge() =>
        FeaturedBadge.IsVisible = ShowFeaturedBadge && _product?.IsFeatured == true;
}
