using DhaqaleysteApp.ViewModels;

namespace DhaqaleysteApp.Pages;

/// <summary>Customer tab: browse products by category. (Staff manage categories in ManageCategoriesPage.)</summary>
public partial class CategoriesPage : ContentPage
{
    private readonly CategoriesViewModel _viewModel;

    public CategoriesPage(CategoriesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearingAsync();
    }
}
