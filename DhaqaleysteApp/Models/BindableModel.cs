using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DhaqaleysteApp.Models;

/// <summary>Minimal change-notification base for models shown in lists.</summary>
public abstract class BindableModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool Set<T>(ref T field, T value, [CallerMemberName] string name = "", params string[] alsoChanged)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(name);
        foreach (var other in alsoChanged)
            OnPropertyChanged(other);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
