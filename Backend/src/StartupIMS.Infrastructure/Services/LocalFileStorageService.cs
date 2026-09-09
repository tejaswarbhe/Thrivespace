using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using StartupIMS.Shared.Settings;

namespace StartupIMS.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _rootPath;

    public LocalFileStorageService(IOptions<FileStorageSettings> settings)
    {
        _rootPath = Path.GetFullPath(settings.Value.RootPath);
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(IFormFile file, string subfolder)
    {
        var folder = Path.Combine(_rootPath, subfolder);
        Directory.CreateDirectory(folder);

        // Generated name, never the client's original file name - avoids
        // path traversal (e.g. "../../evil.exe") and name collisions.
        var extension = Path.GetExtension(file.FileName);
        var storedFileName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(folder, storedFileName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        // Returning absolute path as requested.
        return fullPath;
    }

    public Stream OpenRead(string relativePath)
    {
        var fullPath = Path.Combine(_rootPath, relativePath);
        return new FileStream(fullPath, FileMode.Open, FileAccess.Read);
    }

    public void Delete(string relativePath)
    {
        var fullPath = Path.Combine(_rootPath, relativePath);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }
}
