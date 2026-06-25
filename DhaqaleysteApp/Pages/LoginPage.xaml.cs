using DhaqaleysteApp.Services;

namespace DhaqaleysteApp.Pages;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    private async void Login_Clicked(object sender, EventArgs e)
    {
        var api = new ApiService();

        var user = await api.LoginAsync(
            EmailEntry.Text ?? "",
            PasswordEntry.Text ?? "");

        if (user == null)
        {
            await DisplayAlert("Error", "Invalid email or password", "OK");
            return;
        }


        // SAVE SESSION 
        await SessionService.SaveUserAsync(
            user.UserId,
            user.FullName,
            user.Role);


        if (user.Role == "ADMIN")
        {
            await DisplayAlert(
                "Welcome",
                $"Admin: {user.FullName}",
                "OK");

            await Navigation.PushAsync(
                new AdminDashboardPage());
        }
        else
        {
            await DisplayAlert(
                "Welcome",
                user.FullName,
                "OK");

            await Navigation.PopToRootAsync();
        }
    }

    private async void Register_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegisterPage());
    }
}