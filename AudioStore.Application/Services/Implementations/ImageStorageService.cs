using AudioStore.Common.Services.Interfaces;

namespace AudioStore.Application.Services.Implementations;

/// <summary>
/// Service for saving uploaded images (base64) to disk and returning URL paths.
/// Images are stored in wwwroot/images/{subfolder}/{guid}.jpg
/// </summary>
public class ImageStorageService : IImageStorageService
{
    private readonly string _wwwRootPath;

    public ImageStorageService(string wwwRootPath)
    {
        _wwwRootPath = wwwRootPath;
    }

    /// <inheritdoc/>
    public async Task<string> SaveImageAsync(string imageData, string subfolder)
    {
        // If it's already a URL path (not base64), return as-is
        if (!IsBase64Image(imageData))
            return imageData;

        // Extract the raw base64 bytes
        var base64Data = ExtractBase64Data(imageData);
        var imageBytes = Convert.FromBase64String(base64Data);

        // Generate unique filename
        var fileName = $"{Guid.NewGuid()}.jpg";
        var relativePath = $"/images/{subfolder}/{fileName}";
        var absolutePath = Path.Combine(_wwwRootPath, "images", subfolder, fileName);

        // Ensure directory exists
        var directory = Path.GetDirectoryName(absolutePath)!;
        Directory.CreateDirectory(directory);

        // Write file to disk
        await File.WriteAllBytesAsync(absolutePath, imageBytes);

        return relativePath;
    }

    /// <inheritdoc/>
    public async Task<List<string>> SaveImagesAsync(List<string> imageDataList, string subfolder)
    {
        var results = new List<string>();
        foreach (var imageData in imageDataList)
        {
            var url = await SaveImageAsync(imageData, subfolder);
            results.Add(url);
        }
        return results;
    }

    /// <inheritdoc/>
    public void DeleteImage(string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl) || IsBase64Image(imageUrl))
            return;

        // Convert URL path to absolute file path
        // imageUrl format: /images/products/abc.jpg
        var relativePath = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var absolutePath = Path.Combine(_wwwRootPath, relativePath);

        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }
    }

    /// <summary>
    /// Checks if the string is a base64 data URL (data:image/...;base64,...)
    /// </summary>
    private static bool IsBase64Image(string data)
    {
        return !string.IsNullOrEmpty(data) && data.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Extracts the raw base64 string from a data URL
    /// Input:  "data:image/jpeg;base64,/9j/4AAQ..."
    /// Output: "/9j/4AAQ..."
    /// </summary>
    private static string ExtractBase64Data(string dataUrl)
    {
        var commaIndex = dataUrl.IndexOf(',');
        return commaIndex >= 0 ? dataUrl[(commaIndex + 1)..] : dataUrl;
    }
}
