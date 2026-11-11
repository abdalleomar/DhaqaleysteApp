using DhaqaleysteApp.Services;

namespace DhaqaleysteApp.Pages;

/// <summary>Step 1: email gets a 6-digit code. Step 2: code + new password.</summary>
public partial class ForgotPasswordPage : ContentPage
{
    private readonly ApiService _api = ServiceHelper.Get<ApiService>();
    private bool _codeSent;

    public ForgotPasswordPage(string? email = null)
    {
        InitializeComponent();
        if (email != null && email.Contains('@'))
            EmailEntry.Text = email;
        SubmitButton.Text = "Send code";
    }

    private async void Submit_Clicked(object? sender, EventArgs e)
    {
        if (Busy.IsRunning)
            return;

        if (_codeSent)
            await ResetAsync();
        else
            await SendCodeAsync();
    }

    private async Task SendCodeAsync()
    {
        var email = EmailEntry.Text?.Trim() ?? "";
        if (!email.Contains('@'))
        {
            ShowError("Enter the email of your staff account.");
            return;
        }

        SetBusy(true);
        try
        {
            var result = await _api.ForgotPasswordAsync(email);
            if (!result.Success)
            {
                ShowError(result.Error);
                return;
            }

            _codeSent = true;
            CodeSection.IsVisible = true;
            EmailEntry.IsEnabled = false;
            SubtitleLabel.Text = $"If {email} has a staff account, a 6-digit code is on its way. It works for 15 minutes.";
            SubmitButton.Text = "Set new password";
            CodeEntry.Focus();
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task ResetAsync()
    {
        var email = EmailEntry.Text?.Trim() ?? "";
        var code = CodeEntry.Text?.Trim() ?? "";
        var password = NewPasswordEntry.Text ?? "";
        var error = code.Length != 6 || !code.All(char.IsDigit)
            ? "Enter the 6-digit code from the email."
            : FormHelpers.CheckNewPassword(password, ConfirmEntry.Text ?? "");
        if (error != null)
        {
            ShowError(error);
            return;
        }

        SetBusy(true);
        try
        {
            var result = await _api.ResetPasswordAsync(email, code, password);
            if (!result.Success)
            {
                ShowError(result.Error);
                return;
            }

            await DisplayAlert("Password changed", "Sign in with your new password.", "OK");
            await Navigation.PopAsync();
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void Resend_Tapped(object? sender, TappedEventArgs e)
    {
        _codeSent = false;
        await SendCodeAsync();
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
