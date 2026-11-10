using System.Globalization;

namespace DhaqaleysteApp.Services.Localization;

public enum AppLanguage { English, Somali }

/// <summary>
/// English / Somali text for the customer screens.
///
/// All strings live in <see cref="Strings"/> below, English and Somali side by side, so a native
/// speaker can review and correct the Somali in one place. Missing Somali text falls back to English.
/// </summary>
public static class Loc
{
    private const string PreferenceKey = "Language";

    public static AppLanguage Language { get; private set; } = AppLanguage.English;

    public static bool IsSomali => Language == AppLanguage.Somali;

    /// <summary>Loads the saved language, or picks Somali when the phone is set to Somali.</summary>
    public static void Initialize()
    {
        var saved = Preferences.Default.Get(PreferenceKey, "");
        if (Enum.TryParse<AppLanguage>(saved, out var language))
            Language = language;
        else
            Language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "so" ? AppLanguage.Somali : AppLanguage.English;
    }

    public static void SetLanguage(AppLanguage language)
    {
        Language = language;
        Preferences.Default.Set(PreferenceKey, language.ToString());
    }

    /// <summary>Translated text for a key. Unknown keys return the key itself so they are easy to spot.</summary>
    public static string T(string key)
    {
        if (!Strings.TryGetValue(key, out var text))
            return key;
        return IsSomali && !string.IsNullOrEmpty(text.So) ? text.So : text.En;
    }

    public static string T(string key, params object[] args) => string.Format(CultureInfo.InvariantCulture, T(key), args);

    /// <summary>Picks the Somali value when Somali is selected and the value exists (e.g. category names).</summary>
    public static string Pick(string english, string? somali) =>
        IsSomali && !string.IsNullOrWhiteSpace(somali) ? somali : english;

    public static IReadOnlyCollection<string> Keys => Strings.Keys;

    // NOTE: Somali translations should be reviewed by a native speaker before release.
    private static readonly Dictionary<string, (string En, string So)> Strings = new()
    {
        // Tabs and titles
        ["TabHome"] = ("Home", "Hoyga"),
        ["TabCategories"] = ("Categories", "Qaybaha"),
        ["TabFavorites"] = ("Favorites", "Jeclahay"),
        ["TabContact"] = ("Contact", "Nala xiriir"),
        ["ProductTitle"] = ("Product", "Alaab"),
        ["FiltersTitle"] = ("Filters", "Shaandhee"),

        // Home
        ["SearchPlaceholder"] = ("Search products…", "Raadi alaab…"),
        ["Featured"] = ("Featured", "Alaabta la xushay"),
        ["NewArrivals"] = ("New arrivals", "Alaabta cusub"),
        ["SeeAll"] = ("See all", "Dhammaan eeg"),
        ["RecentlyViewed"] = ("Recently viewed", "Waxaad dhowaan eegtay"),
        ["All"] = ("All", "Dhammaan"),
        ["AllProducts"] = ("All products", "Dhammaan alaabta"),
        ["SearchResults"] = ("Search results", "Natiijooyinka raadinta"),

        // Listing, filters, sorting
        ["ResultsCount"] = ("{0} products", "{0} alaab"),
        ["Filters"] = ("Filters", "Shaandhee"),
        ["Sort"] = ("Sort", "Kala saar"),
        ["SortNewest"] = ("Newest first", "Kuwa ugu cusub"),
        ["SortPriceAsc"] = ("Price: low to high", "Qiimaha: jaban ilaa qaali"),
        ["SortPriceDesc"] = ("Price: high to low", "Qiimaha: qaali ilaa jaban"),
        ["SortName"] = ("Name (A–Z)", "Magaca (A–Z)"),
        ["SortPopular"] = ("Most popular", "Kuwa ugu caansan"),
        ["InStockOnly"] = ("Only show items in stock", "Kaliya kuwa la heli karo"),
        ["PriceRange"] = ("Price range", "Inta u dhaxaysa qiimaha"),
        ["MinPrice"] = ("Min", "Ugu yar"),
        ["MaxPrice"] = ("Max", "Ugu badan"),
        ["Category"] = ("Category", "Qaybta"),
        ["ShowResults"] = ("Show results", "Muuji natiijooyinka"),
        ["Reset"] = ("Reset", "Dib u dejin"),
        ["InvalidPriceRange"] = ("Enter valid prices. Min must not be more than max.", "Geli qiimo sax ah. Ka ugu yar waa inuusan ka badnaan ka ugu badan."),

        // States
        ["NoProductsTitle"] = ("No products yet", "Alaab weli ma jirto"),
        ["NoProductsMessage"] = ("Please check back soon.", "Fadlan dib noo soo eeg dhowaan."),
        ["NoResultsTitle"] = ("No matching products", "Wax la helay ma jiraan"),
        ["NoResultsMessage"] = ("Try another search or remove some filters.", "Isku day raadin kale ama ka saar shaandhada qaarkood."),
        ["ErrorTitle"] = ("Something went wrong", "Wax baa khaldamay"),
        ["TryAgain"] = ("Try again", "Isku day mar kale"),
        ["CouldNotRefresh"] = ("Could not refresh", "Lama cusboonaysiin karin"),
        ["Ok"] = ("OK", "Hagaag"),
        ["Cancel"] = ("Cancel", "Jooji"),

        // Product
        ["InStock"] = ("In stock", "Waa la heli karaa"),
        ["OutOfStock"] = ("Out of stock", "Waa dhammaaday"),
        ["SoldOut"] = ("Sold out", "Lama heli karo"),
        ["FeaturedBadge"] = ("Featured", "La xushay"),
        ["Description"] = ("Description", "Faahfaahin"),
        ["Videos"] = ("Videos", "Muuqaallo"),
        ["PlayVideo"] = ("Play video", "Daar muuqaalka"),
        ["PlayVideoNumber"] = ("Play video {0}", "Daar muuqaalka {0}"),
        ["Call"] = ("Call", "Wac"),
        ["WhatsApp"] = ("WhatsApp", "WhatsApp"),
        ["Share"] = ("Share", "La wadaag"),
        ["AddFavorite"] = ("Save to favorites", "Ku kaydi kuwa aad jeceshahay"),
        ["RemoveFavorite"] = ("Remove from favorites", "Ka saar kuwa aad jeceshahay"),
        ["WhatsAppMessage"] = ("Hello, I am interested in \"{0}\" ({1}).", "Salaan, waxaan xiiseynayaa \"{0}\" ({1})."),
        ["ProductNotFound"] = ("This product is no longer available.", "Alaabtan hadda lama heli karo."),

        // Categories
        ["ProductsCount"] = ("{0} products", "{0} alaab"),
        ["NoCategoriesTitle"] = ("No categories yet", "Qaybo weli ma jiraan"),

        // Favorites
        ["FavoritesTitle"] = ("Favorites", "Kuwa aan jeclahay"),
        ["FavoritesEmptyTitle"] = ("No favorites yet", "Wax aad jeceshahay weli ma jiraan"),
        ["FavoritesEmptyMessage"] = ("Tap the heart on any product to save it here.", "Taabo wadnaha alaab kasta si aad halkan ugu kaydiso."),
        ["FavoritesOffline"] = ("Showing saved details. Prices may have changed.", "Waxaa la muujinayaa xogtii la kaydiyay. Qiimaha waa isbeddeli karaa."),
        ["BrowseProducts"] = ("Browse products", "Daawo alaabta"),

        // Contact
        ["ContactTitle"] = ("Contact us", "Nala xiriir"),
        ["Phone"] = ("Phone", "Telefoon"),
        ["Email"] = ("Email", "Iimayl"),
        ["Address"] = ("Address", "Cinwaan"),
        ["WorkingHours"] = ("Working hours", "Saacadaha shaqada"),
        ["FollowUs"] = ("Follow us", "Na raac"),
        ["Website"] = ("Website", "Websaydh"),
        ["Settings"] = ("Settings", "Dejinta"),
        ["Language"] = ("Language", "Luqadda"),
        ["Theme"] = ("Appearance", "Muuqaalka"),
        ["ThemeSystem"] = ("Same as phone", "Sida taleefanka"),
        ["ThemeLight"] = ("Light", "Iftiin"),
        ["ThemeDark"] = ("Dark", "Mugdi"),
        ["StaffSignIn"] = ("Staff sign in", "Gelitaanka shaqaalaha"),
        ["StaffDashboard"] = ("Staff dashboard", "Shaqaalaha"),
        ["English"] = ("English", "English"),
        ["Somali"] = ("Soomaali", "Soomaali"),
        ["NotAvailable"] = ("Not available", "Lama heli karo"),
        ["CannotOpen"] = ("This could not be opened on this device.", "Tan laguma furi karo qalabkan."),

        // Errors shown to customers (ApiService)
        ["ErrNoInternet"] = ("No internet connection. Check your connection and try again.", "Internet ma jiro. Hubi xiriirkaaga oo isku day mar kale."),
        ["ErrUnreachable"] = ("Could not reach the server. Check your connection and try again.", "Server-ka lama gaari karo. Hubi xiriirkaaga oo isku day mar kale."),
        ["ErrServer"] = ("The server had a problem. Please try again later.", "Server-ka ayaa dhib qabay. Fadlan isku day mar dambe."),
        ["ErrNotConfigured"] = ("The app is not connected to a server. Please contact the company.", "App-ka kuma xirna server. Fadlan la xiriir shirkadda."),
        ["ErrTooMany"] = ("Too many attempts. Please wait a minute and try again.", "Isku day badan. Fadlan sug daqiiqad oo isku day mar kale."),
        ["ErrUnexpected"] = ("The server sent an unexpected response.", "Server-ka ayaa soo diray jawaab aan la filayn."),
        ["ErrNotFound"] = ("This item no longer exists.", "Waxan hadda ma jiro."),
    };
}
