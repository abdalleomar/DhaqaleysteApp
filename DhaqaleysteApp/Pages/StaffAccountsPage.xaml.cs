using System.Collections.ObjectModel;
using DhaqaleysteApp.Models;
using DhaqaleysteApp.Services;

namespace DhaqaleysteApp.Pages;

/// <summary>Admins: share the staff invite code and change or remove staff accounts.</summary>
public partial class StaffAccountsPage : ContentPage
{
    private readonly ApiService _api = ServiceHelper.Get<ApiService>();
    private readonly SessionService _session = ServiceHelper.Get<SessionService>();
    private string _code = "";
    private int _myUserId;

    public ObservableCollection<StaffUser> Staff { get; } = new();

    public StaffAccountsPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _myUserId = await _session.GetUserIdAsync();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        ErrorLabel.IsVisible = false;
        var codeTask = _api.GetInviteCodeAsync();
        var staffTask = _api.GetStaffAsync();
        await Task.WhenAll(codeTask, staffTask);
        Busy.IsVisible = Busy.IsRunning = false;
        Refresh.IsRefreshing = false;

        if (codeTask.Result.Success)
            ShowCode(codeTask.Result.Data!.Code);

        if (!staffTask.Result.Success)
        {
            ErrorLabel.Text = staffTask.Result.Error;
            ErrorLabel.IsVisible = true;
            return;
        }

        Staff.Clear();
        foreach (var person in staffTask.Result.Data!.OrderBy(p => p.IsRemoved).ThenByDescending(p => p.IsAdmin).ThenBy(p => p.FullName))
            Staff.Add(person);
    }

    private void ShowCode(string code)
    {
        _code = code;
        var on = code.Length > 0;
        CodeLabel.Text = on ? code : "Sign-up is off";
        CodeLabel.FontSize = on ? 30 : 22;
        CodeHint.Text = on
            ? "Share this with new staff. They enter it on \"Create your staff account\". Make a new code when someone leaves."
            : "Nobody can create a staff account. Tap \"New code\" to allow sign-up again.";
        ShareButton.IsEnabled = on;
        TurnOffLabel.IsVisible = on;
    }

    private async void Share_Clicked(object? sender, EventArgs e)
    {
        if (_code.Length == 0)
            return;
        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = "Dhaqaleyste staff invite code",
            Text = $"Dhaqaleyste staff invite code: {_code}\nOpen the app → Contact → Staff sign in → Create your staff account."
        });
    }

    private async void NewCode_Clicked(object? sender, EventArgs e)
    {
        if (_code.Length > 0 &&
            !await DisplayAlert("New code", "Make a new invite code? The current code stops working. Existing accounts are not affected.", "Make new code", "Cancel"))
            return;

        var result = await _api.NewInviteCodeAsync();
        if (result.Success)
            ShowCode(result.Data!.Code);
        else
            await DisplayAlert("Not changed", result.Error, "OK");
    }

    private async void TurnOff_Tapped(object? sender, TappedEventArgs e)
    {
        if (!await DisplayAlert("Turn sign-up off", "Nobody will be able to create a new staff account until you make a new code.", "Turn off", "Cancel"))
            return;

        var result = await _api.DisableInviteCodeAsync();
        if (result.Success)
            ShowCode("");
        else
            await DisplayAlert("Not changed", result.Error, "OK");
    }

    private async void Person_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is not BindableObject { BindingContext: StaffUser person })
            return;

        if (person.UserId == _myUserId)
        {
            await DisplayAlert(person.FullName, "This is your own account. Another admin can change it.", "OK");
            return;
        }

        var roleAction = person.IsAdmin ? "Make staff (not admin)" : "Make admin";
        var statusAction = person.IsActive ? "Remove account" : "Restore account";
        var options = person.IsActive && person.Role != "Legacy" ? new[] { roleAction } : Array.Empty<string>();
        var choice = await DisplayActionSheet(person.FullName, "Cancel", statusAction, options);

        StaffUser? updated = null;
        if (choice == statusAction)
        {
            var message = person.IsActive
                ? $"{person.FullName} will be signed out and can no longer sign in. Products they added stay. You can restore the account later."
                : $"{person.FullName} will be able to sign in again with their password (or reset it with Forgot password).";
            if (!await DisplayAlert(statusAction, message, statusAction, "Cancel"))
                return;
            var role = person.Role == "Legacy" ? "Staff" : person.Role;
            var result = await _api.UpdateStaffAsync(person, role, !person.IsActive);
            if (!result.Success) { await DisplayAlert("Not changed", result.Error, "OK"); return; }
            updated = result.Data;
        }
        else if (choice == roleAction)
        {
            var result = await _api.UpdateStaffAsync(person, person.IsAdmin ? "Staff" : "Admin", true);
            if (!result.Success) { await DisplayAlert("Not changed", result.Error, "OK"); return; }
            updated = result.Data;
        }

        if (updated != null)
            await LoadAsync();
    }

    private async void Refresh_Refreshing(object? sender, EventArgs e) => await LoadAsync();

    private async void Back_Tapped(object? sender, TappedEventArgs e) => await Navigation.PopAsync();
}
