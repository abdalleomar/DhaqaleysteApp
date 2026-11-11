using DhaqaleysteApp.Services;

namespace DhaqaleysteApp.Pages;

/// <summary>A new staff member creates their own account with the admin's invite code.</summary>
public partial class RegisterStaffPage : ContentPage
{
    private readonly ApiService _api = ServiceHelper.Get<ApiService>();

    public RegisterStaffPage()
    {
        InitializeComponent();
        SubmitButton.Text = "Create account";
    }

    private async void Submit_Clicked(object? sender, EventArgs e)
    {
        if (Busy.IsRunning)
            return;

        var code = CodeEntry.Text?.Trim() ?? "";
        var name = NameEntry.Text?.Trim() ?? "";
        var email = EmailEntry.Text?.Trim() ?? "";
        var password = PasswordEntry.Text ?? "";

        string? error = code.Length == 0 ? "Enter the staff invite code from your admin."
            : name.Length < 2 ? "Enter your full name."
            : !email.Contains('@') ? "Enter a valid email address."
            : FormHelpers.CheckNewPassword(password, ConfirmEntry.Text ?? "");
        if (error != null)
        {
            ShowError(error);
            return;
        }

        SetBusy(true);
        try
        {
            var result = await _api.RegisterStaffAsync(name, email, PhoneEntry.Text?.Trim() ?? "", password, code);
            if (!result.Success)
            {
                ShowError(result.Error);
                return;
            }

            await DisplayAlert("Welcome", $"Your staff account is ready, {name}.", "OK");
            // Signed in: go to the dashboard, with the customer screen behind it.
            var stack = Navigation.NavigationStack.ToList();
            Navigation.InsertPageBefore(new AdminDashboardPage(), this);
            foreach (var page in stack.Where(p => p is LoginPage))
                Navigation.RemovePage(page);
            await Navigation.PopAsync();
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
