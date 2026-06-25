namespace DhaqaleysteApp.Pages;

public partial class AdminLoginPage : ContentPage
{
    public AdminLoginPage()
    {
        InitializeComponent();
    }

    private async void Login_Clicked(object sender, EventArgs e)
    {
        if (UsernameEntry.Text == "admin" &&
            PasswordEntry.Text == "1234")
        {
            await Navigation.PushAsync(new AdminDashboardPage());
        }
        else
        {
            await DisplayAlert("Error", "Invalid username or password", "OK");
        }
    }
}