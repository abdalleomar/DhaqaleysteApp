using DhaqaleysteApp.ViewModels;

namespace DhaqaleysteApp.Pages;

public partial class ProductListPage : ContentPage
{
    private readonly ProductListViewModel _viewModel;

    public ProductListPage(ProductListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel.FocusSearch)
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(300), () => Search.Focus());
        await _viewModel.OnAppearingAsync();
    }
}
