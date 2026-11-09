using DhaqaleysteApp.Services;
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
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            builder.Services.AddSingleton(AppConfig.Load());
            builder.Services.AddSingleton<SessionService>();
            builder.Services.AddTransient<AuthHandler>();

            // One shared, pooled HttpClient for the whole app.
            builder.Services
                .AddHttpClient<ApiService>()
                .AddHttpMessageHandler<AuthHandler>();

            return builder.Build();
        }
    }
}
