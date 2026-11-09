namespace DhaqaleysteApi.Options
{
    public class StorageOptions
    {
        public const string Section = "Storage";

        /// <summary>Folder for uploaded media. Relative paths are resolved against the content root.</summary>
        public string RootPath { get; set; } = "uploads";

        public long MaxImageBytes { get; set; } = 5 * 1024 * 1024;
        public long MaxVideoBytes { get; set; } = 50 * 1024 * 1024;
    }
}
