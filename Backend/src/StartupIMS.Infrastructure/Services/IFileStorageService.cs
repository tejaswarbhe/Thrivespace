using Microsoft.AspNetCore.Http;

namespace StartupIMS.Infrastructure.Services;

public interface IFileStorageService
{
    /// <summary>
    /// Saves the uploaded file under the given subfolder and returns the
    /// relative path to store in the database (not the original file name -
    /// a generated unique name, to avoid collisions and path traversal).
    /// </summary>
    Task<string> SaveAsync(IFormFile file, string subfolder);

    /// <summary>
    /// Opens the stored file for reading, given the relative path saved earlier.
    /// </summary>
    Stream OpenRead(string relativePath);

    void Delete(string relativePath);
}
