namespace DhaqaleysteApp.Services;

/// <summary>
/// Tells customer screens that staff changed the catalogue (products, categories or company details),
/// so they reload the next time they appear instead of showing old data.
/// </summary>
public static class CatalogEvents
{
    public static int Version { get; private set; }

    public static void Changed() => Version++;
}
