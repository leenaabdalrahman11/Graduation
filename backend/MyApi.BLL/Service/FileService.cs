using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace MyApi.BLL.Service;

public class FileService : IFileService
{
    private readonly IWebHostEnvironment _environment;
    private readonly string _imagesFolder;

    public FileService(IWebHostEnvironment environment)
    {
        _environment = environment;

        var webRootPath = _environment.WebRootPath;

        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            webRootPath = Path.Combine(
                _environment.ContentRootPath,
                "wwwroot"
            );
        }

        _imagesFolder = Path.Combine(webRootPath, "images");

        Directory.CreateDirectory(_imagesFolder);
    }

    public async Task<string?> UploadAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new Exception("File is null or empty.");

        if (string.IsNullOrWhiteSpace(file.ContentType) ||
            !file.ContentType.StartsWith(
                "image/",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("The uploaded file is not an image.");
        }

        var extension = Path.GetExtension(file.FileName);

        if (string.IsNullOrWhiteSpace(extension))
            throw new Exception("The image extension is missing.");

        var allowedExtensions = new[]
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp",
            ".gif"
        };

        if (!allowedExtensions.Contains(
                extension,
                StringComparer.OrdinalIgnoreCase))
        {
            throw new Exception("Unsupported image extension.");
        }

        var uniqueFileName =
            $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";

        var fullPath = Path.Combine(
            _imagesFolder,
            uniqueFileName
        );

        await using var stream = new FileStream(
            fullPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None
        );

        await file.CopyToAsync(stream);

        
        return uniqueFileName;
    }

    public Task DeleteAsync(string fileNameOrUrl)
    {
        if (string.IsNullOrWhiteSpace(fileNameOrUrl))
            return Task.CompletedTask;

        try
        {
            var fileName = GetFileName(fileNameOrUrl);

            if (string.IsNullOrWhiteSpace(fileName))
                return Task.CompletedTask;

            var fullPath = Path.Combine(
                _imagesFolder,
                fileName
            );

            
            var normalizedImagesFolder =
                Path.GetFullPath(_imagesFolder);

            var normalizedFilePath =
                Path.GetFullPath(fullPath);

            if (!normalizedFilePath.StartsWith(
                    normalizedImagesFolder,
                    StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }

            if (File.Exists(normalizedFilePath))
                File.Delete(normalizedFilePath);
        }
        catch
        {
        }

        return Task.CompletedTask;
    }

    public async Task<string?> UploadLocalFileAsync(
        string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) ||
            !File.Exists(filePath))
        {
            throw new Exception(
                "File path is invalid or file does not exist."
            );
        }

        var extension = Path.GetExtension(filePath);

        if (string.IsNullOrWhiteSpace(extension))
            throw new Exception("The image extension is missing.");

        var allowedExtensions = new[]
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp",
            ".gif"
        };

        if (!allowedExtensions.Contains(
                extension,
                StringComparer.OrdinalIgnoreCase))
        {
            throw new Exception("Unsupported image extension.");
        }

        var uniqueFileName =
            $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";

        var destinationPath = Path.Combine(
            _imagesFolder,
            uniqueFileName
        );

        await using var sourceStream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read
        );

        await using var destinationStream = new FileStream(
            destinationPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None
        );

        await sourceStream.CopyToAsync(destinationStream);

        return uniqueFileName;
    }

    private static string GetFileName(string fileNameOrUrl)
    {
        

        if (Uri.TryCreate(
                fileNameOrUrl,
                UriKind.Absolute,
                out var uri))
        {
            return Path.GetFileName(uri.LocalPath);
        }

        return Path.GetFileName(fileNameOrUrl);
    }
}