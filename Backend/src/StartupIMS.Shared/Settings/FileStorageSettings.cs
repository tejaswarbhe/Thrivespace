namespace StartupIMS.Shared.Settings;

public class FileStorageSettings
{
    public const string SectionName = "FileStorage";

    // Relative or absolute path where uploaded files are stored on disk.
    // Kept outside wwwroot on purpose - files are only ever served through
    // an authenticated endpoint, never as static/public files.
    public string RootPath { get; set; } = "App_Data/uploads";

    public long MaxFileSizeBytes { get; set; } = 20 * 1024 * 1024; // 20 MB
}
