using Microsoft.AspNetCore.WebUtilities;
using Voltflow.Application.Common;

namespace Voltflow.Api.Pagination;

/// <summary>V8: RFC 8288 Link header (rel="next"/"prev"/"first"/"last"), built from the same
/// limit/offset the query was actually served with - never invented, never a different scheme.</summary>
public static class PaginationLinkHeaderExtensions
{
    public static void ApplyPaginationHeaders<T>(this HttpContext httpContext, PagedResult<T> page)
    {
        var request = httpContext.Request;
        var basePath = $"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}";
        var otherQuery = QueryHelpers.ParseQuery(request.QueryString.Value ?? string.Empty)
            .Where(kvp => !string.Equals(kvp.Key, "limit", StringComparison.OrdinalIgnoreCase)
                       && !string.Equals(kvp.Key, "offset", StringComparison.OrdinalIgnoreCase))
            .SelectMany(kvp => kvp.Value, (kvp, value) => new KeyValuePair<string, string?>(kvp.Key, value));

        string BuildUrl(int offset) =>
            QueryHelpers.AddQueryString(
                QueryHelpers.AddQueryString(basePath, otherQuery),
                new Dictionary<string, string?> { ["limit"] = page.Limit.ToString(), ["offset"] = offset.ToString() });

        var links = new List<string> { $"<{BuildUrl(0)}>; rel=\"first\"" };
        if (page.HasPrevious) links.Add($"<{BuildUrl(Math.Max(0, page.Offset - page.Limit))}>; rel=\"prev\"");
        if (page.HasNext) links.Add($"<{BuildUrl(page.Offset + page.Limit)}>; rel=\"next\"");
        var lastOffset = page.TotalCount == 0 ? 0 : ((page.TotalCount - 1) / page.Limit) * page.Limit;
        links.Add($"<{BuildUrl(lastOffset)}>; rel=\"last\"");

        httpContext.Response.Headers.Append("Link", string.Join(", ", links));
        httpContext.Response.Headers.Append("X-Total-Count", page.TotalCount.ToString());
    }
}
