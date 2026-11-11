using DhaqaleysteApp.Services;
using DhaqaleysteApp.Services.Localization;

namespace DhaqaleysteApp.Views;

/// <summary>
/// The app's bottom navigation (Home · Categories · Favorites · Contact), drawn by the app so it looks the
/// same on Android, iOS and Windows: the active tab gets a rounded highlight behind its icon.
/// Set <see cref="Current"/> to the route of the tab the page belongs to.
/// </summary>
public class TabBarView : ContentView
{
    public static readonly BindableProperty CurrentProperty =
        BindableProperty.Create(nameof(Current), typeof(string), typeof(TabBarView), "home", propertyChanged: (b, _, _) => ((TabBarView)b).Build());

    private static readonly (string Route, string Icon, string TextKey)[] Tabs =
    {
        ("home", "\U000F06A1", "TabHome"),
        ("categories", "\U000F11D9", "TabCategories"),
        ("favorites", "\U000F02D5", "TabFavorites"),
        ("contact", "\U000F0EDE", "TabContact")
    };

    public TabBarView() => Build();

    public string Current
    {
        get => (string)GetValue(CurrentProperty);
        set => SetValue(CurrentProperty, value);
    }

    private void Build()
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection(
                new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star)),
            Padding = new Thickness(0, 10, 0, 12)
        };

        for (var i = 0; i < Tabs.Length; i++)
        {
            var (route, icon, textKey) = Tabs[i];
            var active = route == Current;
            var glyph = active && route == "favorites" ? "\U000F02D1" : icon;   // filled heart when active

            var iconLabel = new Label { Text = glyph, FontFamily = "Icons", FontSize = 22, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
            iconLabel.SetDynamicResource(Label.TextColorProperty, active ? "PrimaryColor" : "TextTertiaryColor");

            var pill = new Border
            {
                WidthRequest = 56,
                HeightRequest = 30,
                StrokeThickness = 0,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 15 },
                HorizontalOptions = LayoutOptions.Center,
                Content = iconLabel
            };
            if (active)
                pill.SetDynamicResource(BackgroundColorProperty, "PrimarySoftColor");
            else
                pill.BackgroundColor = Colors.Transparent;

            var text = new Label
            {
                Text = Loc.T(textKey),
                FontSize = 11,
                FontFamily = active ? "BodySemibold" : "BodyMedium",
                HorizontalOptions = LayoutOptions.Center,
                MaxLines = 1,
                LineBreakMode = LineBreakMode.TailTruncation
            };
            text.SetDynamicResource(Label.TextColorProperty, active ? "PrimaryColor" : "TextTertiaryColor");

            var item = new VerticalStackLayout { Spacing = 4, Children = { pill, text } };
            SemanticProperties.SetDescription(item, Loc.T(textKey));
            var target = route;
            item.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await GoAsync(target)) });
            grid.Add(item, i);
        }

        var bar = new Border
        {
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.Rectangle(),
            Content = new VerticalStackLayout
            {
                Children = { new BoxView { HeightRequest = 1, Style = (Style)Application.Current!.Resources["Divider"] }, grid }
            }
        };
        bar.SetDynamicResource(BackgroundColorProperty, "SurfaceColor");
        Content = bar;
    }

    private static async Task GoAsync(string route)
    {
        var shell = Shell.Current;
        if (shell == null)
            return;

        // Tapping the tab you are on goes back to its first screen.
        if (shell.CurrentItem?.CurrentItem?.Route == route)
        {
            if (shell.Navigation.NavigationStack.Count > 1)
                await shell.Navigation.PopToRootAsync();
            return;
        }
        await shell.GoToAsync($"//{route}");
    }
}
