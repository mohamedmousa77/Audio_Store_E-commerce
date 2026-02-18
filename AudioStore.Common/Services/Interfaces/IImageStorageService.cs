namespace AudioStore.Common.Services.Interfaces;

/// <summary>
/// Service for managing image storage on disk.
/// Converts base64 data URLs to static files and returns URL paths.
/// </summary>
public interface IImageStorageService
{
    /// <summary>
    /// Save a single image to disk.
    /// If the input is already a URL path (not base64), it is returned unchanged.
    /// </summary>
    /// <param name="imageData">Base64 data URL or existing URL path</param>
    /// <param name="subfolder">Subfolder under /images/ (e.g. "products", "categories")</param>
    /// <returns>URL path to the saved image (e.g. /images/products/abc.jpg)</returns>
    Task<string> SaveImageAsync(string imageData, string subfolder);

    /// <summary>
    /// Save multiple images to disk.
    /// </summary>
    /// <param name="imageDataList">List of base64 data URLs or existing URL paths</param>
    /// <param name="subfolder">Subfolder under /images/</param>
    /// <returns>List of URL paths</returns>
    Task<List<string>> SaveImagesAsync(List<string> imageDataList, string subfolder);

    /// <summary>
    /// Delete an image file from disk given its URL path.
    /// </summary>
    /// <param name="imageUrl">URL path of the image to delete</param>
    void DeleteImage(string imageUrl);
}
