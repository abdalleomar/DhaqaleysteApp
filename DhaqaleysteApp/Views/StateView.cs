using System.Windows.Input;

namespace DhaqaleysteApp.Views;

/// <summary>
/// The one loading / empty / error display used on every customer screen:
/// a spinner while loading, otherwise an icon, a title, a message and an optional action button.
/// </summary>
public class StateView : ContentView
{
    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(StateView), false, propertyChanged: Update);
    public static readonly BindableProperty IconProperty = BindableProperty.Create(nameof(Icon), typeof(string), typeof(StateView), "", propertyChanged: Update);
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(StateView), "", propertyChanged: Update);
    public static readonly BindableProperty MessageProperty = BindableProperty.Create(nameof(Message), typeof(string), typeof(StateView), "", propertyChanged: Update);
    public static readonly BindableProperty ActionTextProperty = BindableProperty.Create(nameof(ActionText), typeof(string), typeof(StateView), "", propertyChanged: Update);
    public static readonly BindableProperty ActionCommandProperty = BindableProperty.Create(nameof(ActionCommand), typeof(ICommand), typeof(StateView), null, propertyChanged: Update);

    private readonly ActivityIndicator _spinner = new() { IsRunning = true, HorizontalOptions = LayoutOptions.Center };
    private readonly Label _icon = new() { FontSize = 56, HorizontalOptions = LayoutOptions.Center };
    private readonly Label _title = new() { FontSize = 18, FontFamily = "OpenSansSemibold", HorizontalTextAlignment = TextAlignment.Center };
    private readonly Label _message = new() { FontSize = 14, HorizontalTextAlignment = TextAlignment.Center };
    private readonly Button _action = new() { HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 8, 0, 0) };

    public StateView()
    {
        _icon.FontFamily = "Icons";
        _icon.SetDynamicResource(Label.TextColorProperty, "TextTertiaryColor");
        _title.SetDynamicResource(Label.TextColorProperty, "TextPrimaryColor");
        _message.SetDynamicResource(Label.TextColorProperty, "TextSecondaryColor");

        Content = new VerticalStackLayout
        {
            Spacing = 8,
            Padding = new Thickness(32, 48),
            VerticalOptions = LayoutOptions.Center,
            Children = { _spinner, _icon, _title, _message, _action }
        };
        Update(this, null, null);
    }

    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); set => SetValue(IsLoadingProperty, value); }
    public string Icon { get => (string)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Message { get => (string)GetValue(MessageProperty); set => SetValue(MessageProperty, value); }
    public string ActionText { get => (string)GetValue(ActionTextProperty); set => SetValue(ActionTextProperty, value); }
    public ICommand? ActionCommand { get => (ICommand?)GetValue(ActionCommandProperty); set => SetValue(ActionCommandProperty, value); }

    private static void Update(BindableObject bindable, object? oldValue, object? newValue)
    {
        var view = (StateView)bindable;
        var loading = view.IsLoading;
        view._spinner.IsVisible = view._spinner.IsRunning = loading;
        view._icon.Text = view.Icon;
        view._icon.IsVisible = !loading && !string.IsNullOrEmpty(view.Icon);
        view._title.Text = view.Title;
        view._title.IsVisible = !loading && !string.IsNullOrEmpty(view.Title);
        view._message.Text = view.Message;
        view._message.IsVisible = !loading && !string.IsNullOrEmpty(view.Message);
        view._action.Text = view.ActionText;
        view._action.Command = view.ActionCommand;
        view._action.IsVisible = !loading && view.ActionCommand != null && !string.IsNullOrEmpty(view.ActionText);
    }
}
