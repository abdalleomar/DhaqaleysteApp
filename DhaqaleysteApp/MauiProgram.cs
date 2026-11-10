using DhaqaleysteApp.Pages;
using DhaqaleysteApp.Services;
using DhaqaleysteApp.ViewModels;
using Microsoft.Extensions.Logging;

namespace DhaqaleysteApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("MaterialIcons-Regular.ttf", "Icons");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            // App services
            builder.Services.AddSingleton(AppConfig.Load());
            builder.Services.AddSingleton<SessionService>();
            builder.Services.AddSingleton<ThemeService>();
            builder.Services.AddSingleton<LocalStore>();
            builder.Services.AddSingleton<NavigationService>();
            builder.Services.AddTransient<AuthHandler>();

            // One shared, pooled HttpClient for the whole app.
            builder.Services
                .AddHttpClient<ApiService>()
                .AddHttpMessageHandler<AuthHandler>();

            // Customer screens (MVVM)
            builder.Services.AddTransient<HomeViewModel>();
            builder.Services.AddTransient<HomePage>();
            builder.Services.AddTransient<CategoriesViewModel>();
            builder.Services.AddTransient<CategoriesPage>();
            builder.Services.AddTransient<FavoritesViewModel>();
            builder.Services.AddTransient<FavoritesPage>();
            builder.Services.AddTransient<ContactViewModel>();
            builder.Services.AddTransient<ContactPage>();
            builder.Services.AddTransient<ProductListViewModel>();
            builder.Services.AddTransient<ProductDetailsViewModel>();

            return builder.Build();
        }
    }
}
