using AutoMapper;
using Microsoft.AspNetCore.Http;

namespace AudioStore.Application.Mapping.Resolvers;

/// <summary>
/// AutoMapper resolver for gallery image lists.
/// Converts each relative path in a List&lt;string&gt; to an absolute URL.
/// Delegates per-image logic to <see cref="ImageUrlResolver"/>.
/// </summary>
public class GalleryImageUrlResolver : IMemberValueResolver<object, object, List<string>?, List<string>?>
{
    private readonly ImageUrlResolver _singleResolver;

    public GalleryImageUrlResolver(IHttpContextAccessor httpContextAccessor)
    {
        _singleResolver = new ImageUrlResolver(httpContextAccessor);
    }

    public List<string>? Resolve(object source, object destination, List<string>? sourceMember,
        List<string>? destMember, ResolutionContext context)
    {
        if (sourceMember == null || sourceMember.Count == 0)
            return sourceMember;

        return sourceMember
            .Select(img => _singleResolver.ResolveUrl(img))
            .ToList();
    }
}
