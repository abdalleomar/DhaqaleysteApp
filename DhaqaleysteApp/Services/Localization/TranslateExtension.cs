namespace DhaqaleysteApp.Services.Localization;

/// <summary>
/// XAML: Text="{loc:Translate Key}". Changing the language rebuilds the app shell,
/// so a one-time lookup is enough.
/// </summary>
[ContentProperty(nameof(Key))]
[AcceptEmptyServiceProvider]
public class TranslateExtension : IMarkupExtension<string>
{
    public string Key { get; set; } = "";

    public string ProvideValue(IServiceProvider serviceProvider) => Loc.T(Key);

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
}
