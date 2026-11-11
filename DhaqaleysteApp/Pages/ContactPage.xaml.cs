using DhaqaleysteApp.ViewModels;

namespace DhaqaleysteApp.Pages;

public partial class ContactPage : ContentPage
{
    private readonly ContactViewModel _viewModel;

    public ContactPage(ContactViewModel viewModel)
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
