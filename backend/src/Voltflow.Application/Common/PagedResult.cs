namespace Voltflow.Application.Common;

/// <summary>V8: every list response carries a ceiling; this is the shape that ceiling is measured on.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Limit, int Offset)
{
    public bool HasNext => Offset + Items.Count < TotalCount;
    public bool HasPrevious => Offset > 0;

    public PagedResult<TOut> Map<TOut>(Func<T, TOut> selector) =>
        new(Items.Select(selector).ToList(), TotalCount, Limit, Offset);
}

/// <summary>V8: the ceiling itself - every paginated endpoint clamps to these, it is never unbounded.</summary>
public static class PaginationDefaults
{
    public const int DefaultLimit = 50;
    public const int MaxLimit = 100;

    public static int NormalizeLimit(int? requested) =>
        requested switch
        {
            null or <= 0 => DefaultLimit,
            > MaxLimit => MaxLimit,
            _ => requested.Value
        };

    public static int NormalizeOffset(int? requested) => requested is null or < 0 ? 0 : requested.Value;
}
