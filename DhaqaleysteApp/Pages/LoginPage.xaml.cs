using DhaqaleysteApp.Services;

namespace DhaqaleysteApp.Pages;

public partial class LoginPage : ContentPage
{
    private readonly ApiService _api = ServiceHelper.Get<ApiService>();

    public LoginPage()
    {
        InitializeComponent();
    }

    private async void Login_Clicked(object? sender, EventArgs e)
    {
        if (Busy.IsRunning)
            return;

        var emailOrPhone = EmailEntry.Text?.Trim() ?? "";
        var password = PasswordEntry.Text ?? "";

        if (emailOrPhone.Length == 0 || password.Length == 0)
        {
            ShowError("Enter your email or phone and your password.");
            return;
        }

        SetBusy(true);
        try
        {
            var result = await _api.LoginAsync(emailOrPhone, password);
            if (!result.Success)
            {
                ShowError(result.Error);
                return;
            }

            PasswordEntry.Text = "";
            // Replace the sign-in page with the dashboard so "back" returns to the catalogue.
            Navigation.InsertPageBefore(new AdminDashboardPage(), this);
            await Navigation.PopAsync();
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    private void SetBusy(bool busy)
    {
        Busy.IsVisible = Busy.IsRunning = busy;
        LoginButton.IsEnabled = !busy;
        if (busy)
            ErrorLabel.IsVisible = false;
    }
}
