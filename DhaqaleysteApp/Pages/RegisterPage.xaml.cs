using DhaqaleysteApp.Models;
using DhaqaleysteApp.Services;

namespace DhaqaleysteApp.Pages;

public partial class RegisterPage : ContentPage
{
    public RegisterPage()
    {
        InitializeComponent();
    }

    private async void Register_Clicked(object sender, EventArgs e)
    {
        if (PasswordEntry.Text != ConfirmPasswordEntry.Text)
        {
            await DisplayAlert("Error", "Passwords do not match", "OK");
            return;
        }

        var user = new User
        {
            FullName = FullNameEntry.Text ?? "",
            Phone = PhoneEntry.Text ?? "",
            Email = EmailEntry.Text ?? "",
            Password = PasswordEntry.Text ?? "",
            Role = "USER"
        };

        var api = new ApiService();
        bool success = await api.RegisterAsync(user);

        if (success)
        {
            await DisplayAlert("Success", "Account created", "OK");
            await Navigation.PopAsync();
        }
        else
        {
            await DisplayAlert("Error", "Account already exists", "OK");
        }
    }
}