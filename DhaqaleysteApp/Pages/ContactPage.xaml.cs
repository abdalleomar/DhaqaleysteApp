using DhaqaleysteApp.ViewModels;

namespace DhaqaleysteApp.Pages;

public partial class ContactPage : ContentPage
{
    private readonly ContactViewModel _viewModel;

    public ContactPage(ContactViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        // The selected language is a filled button; the other one is a soft button.
        var other = viewModel.IsSomali ? EnglishButton : SomaliButton;
        other.Style = (Style)Application.Current!.Resources["SecondaryButton"];
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearingAsync();
    }
}
