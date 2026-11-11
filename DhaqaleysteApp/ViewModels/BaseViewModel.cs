using CommunityToolkit.Mvvm.ComponentModel;
using DhaqaleysteApp.Services;
using DhaqaleysteApp.Services.Localization;

namespace DhaqaleysteApp.ViewModels;

/// <summary>
/// Shared loading / empty / error state. Pages show one StateView bound to these properties,
/// so every screen behaves the same way when loading, empty, offline or failing.
/// </summary>
public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowState), nameof(ShowContent))]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError), nameof(ShowState), nameof(ShowContent))]
    private string _errorMessage = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowState), nameof(ShowContent))]
    private bool _isEmpty;

    [ObservableProperty]
    private bool _isOffline;

    public bool HasError => ErrorMessage.Length > 0;

    /// <summary>True when the state view (spinner, empty or error message) should replace the content.</summary>
    public bool ShowState => IsLoading || HasError || IsEmpty;

    public bool ShowContent => !ShowState;

    public string StateTitle => HasError ? Loc.T("ErrorTitle") : EmptyTitle;
    public string StateMessage => HasError ? ErrorMessage : EmptyMessage;
    public string StateIcon => HasError ? (IsOffline ? Icons.Offline : Icons.Info) : EmptyIcon;

    /// <summary>"Try again" only when something failed; an empty list has nothing to retry.</summary>
    public string StateActionText => HasError ? Loc.T("TryAgain") : "";

    protected virtual string EmptyTitle => Loc.T("NoProductsTitle");
    protected virtual string EmptyMessage => Loc.T("NoProductsMessage");
    protected virtual string EmptyIcon => Icons.Inventory;

    protected void SetError(string message, bool offline = false)
    {
        IsOffline = offline;
        ErrorMessage = message;
        NotifyStateText();
    }

    protected void ClearError()
    {
        IsOffline = false;
        ErrorMessage = "";
        NotifyStateText();
    }

    protected void NotifyStateText()
    {
        OnPropertyChanged(nameof(StateTitle));
        OnPropertyChanged(nameof(StateMessage));
        OnPropertyChanged(nameof(StateIcon));
        OnPropertyChanged(nameof(StateActionText));
    }
}
