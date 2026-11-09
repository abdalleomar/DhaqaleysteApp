namespace DhaqaleysteApp.Services;

/// <summary>
/// Resolves shared services for pages that are still created with "new".
/// Transitional: pages move to constructor injection with the MVVM rework (Phase 5).
/// </summary>
public static class ServiceHelper
{
    public static T Get<T>() where T : notnull =>
        IPlatformApplication.Current!.Services.GetRequiredService<T>();
}
