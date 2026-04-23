using AutoMapper;
using Microsoft.AspNetCore.Http;

namespace AudioStore.Application.Mapping.Resolvers;

/// <summary>
/// AutoMapper resolver that converts relative image paths (e.g. /images/products/abc.jpg)
/// into absolute URLs using the current request's scheme and host.
/// Already-absolute URLs (http/https) and empty values are returned unchanged.
/// </summary>
public class ImageUrlResolver : IMemberValueResolver<object, object, string, string>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ImageUrlResolver(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string Resolve(object source, object destination, string sourceMember, string destMember,
        ResolutionContext context)
    {
        return ResolveUrl(sourceMember);
    }

    internal string ResolveUrl(string? imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl))
            return string.Empty;

        // Already an absolute URL — leave it alone
        if (imageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            imageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return imageUrl;

        // Build base URL from current request
        var request = _httpContextAccessor.HttpContext?.Request;
        if (request == null)
            return imageUrl;

        var baseUrl = $"{request.Scheme}://{request.Host}";
        // Ensure the relative path starts with /
        var relativePath = imageUrl.StartsWith('/') ? imageUrl : $"/{imageUrl}";
        return $"{baseUrl}{relativePath}";
    }
}
