using DhaqaleysteApp.Services;

namespace DhaqaleysteApp.Pages;

/// <summary>The signed-in staff member changes their own password.</summary>
public partial class ChangePasswordPage : ContentPage
{
    private readonly ApiService _api = ServiceHelper.Get<ApiService>();
    private readonly SessionService _session = ServiceHelper.Get<SessionService>();

    public ChangePasswordPage()
    {
        InitializeComponent();
        SubmitButton.Text = "Change password";
    }

    private async void Submit_Clicked(object? sender, EventArgs e)
    {
        if (Busy.IsRunning)
            return;

        var current = CurrentEntry.Text ?? "";
        var password = NewPasswordEntry.Text ?? "";
        var error = current.Length == 0 ? "Enter your current password."
            : FormHelpers.CheckNewPassword(password, ConfirmEntry.Text ?? "");
        if (error == null && password == current)
            error = "The new password must be different from the current one.";
        if (error != null)
        {
            ShowError(error);
            return;
        }

        SetBusy(true);
        try
        {
            var result = await _api.ChangePasswordAsync(current, password);
            if (!result.Success)
            {
                ShowError(result.Error);
                return;
            }

            // The server signs out every session after a password change.
            _session.Clear();
            await DisplayAlert("Password changed", "Sign in again with your new password.", "OK");
            await Navigation.PopToRootAsync();
            await Navigation.PushAsync(new LoginPage());
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void Back_Tapped(object? sender, TappedEventArgs e) => await Navigation.PopAsync();

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    private void SetBusy(bool busy)
    {
        Busy.IsVisible = Busy.IsRunning = busy;
        SubmitButton.IsEnabled = !busy;
        if (busy)
            ErrorLabel.IsVisible = false;
    }
}
