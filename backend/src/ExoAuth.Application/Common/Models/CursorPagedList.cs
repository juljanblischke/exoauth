using System.Text;
using System.Text.Json;

namespace ExoAuth.Application.Common.Models;

public sealed class CursorPagedList<T>
{
    public IReadOnlyList<T> Items { get; }
    public PaginationMeta Pagination { get; }

    private CursorPagedList(IReadOnlyList<T> items, PaginationMeta pagination)
    {
        Items = items;
        Pagination = pagination;
    }

    public static CursorPagedList<T> Create(
        IReadOnlyList<T> items,
        int pageSize,
        Func<T, string> cursorSelector,
        string? currentCursor = null)
    {
        var hasMore = items.Count > pageSize;
        var resultItems = hasMore ? items.Take(pageSize).ToList() : items.ToList();

        string? nextCursor = null;
        if (hasMore && resultItems.Count > 0)
        {
            var lastItem = resultItems[^1];
            nextCursor = EncodeCursor(cursorSelector(lastItem));
        }

        var pagination = PaginationMeta.Create(
            cursor: currentCursor,
            nextCursor: nextCursor,
            hasMore: hasMore,
            pageSize: pageSize
        );

        return new CursorPagedList<T>(resultItems, pagination);
    }

    public static CursorPagedList<T> Empty(int pageSize = 20)
    {
        return new CursorPagedList<T>(
            Array.Empty<T>(),
            PaginationMeta.Empty(pageSize)
        );
    }

    /// <summary>
    /// Creates a CursorPagedList from pre-computed pagination values.
    /// </summary>
    public static CursorPagedList<T> FromItems(
        IReadOnlyList<T> items,
        string? nextCursor,
        bool hasMore,
        int pageSize = 20)
    {
        var pagination = PaginationMeta.Create(
            cursor: null,
            nextCursor: nextCursor,
            hasMore: hasMore,
            pageSize: pageSize
        );

        return new CursorPagedList<T>(items, pagination);
    }

    public static string EncodeCursor(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        return Convert.ToBase64String(bytes);
    }

    public static string? DecodeCursor(string? encodedCursor)
    {
        if (string.IsNullOrEmpty(encodedCursor))
            return null;

        try
        {
            var bytes = Convert.FromBase64String(encodedCursor);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return null;
        }
    }

    public static CursorData? DecodeCursorData(string? encodedCursor)
    {
        var decoded = DecodeCursor(encodedCursor);
        if (decoded is null)
            return null;

        try
        {
            return JsonSerializer.Deserialize<CursorData>(decoded);
        }
        catch
        {
            return null;
        }
    }
}

public sealed record CursorData
{
    public Guid? Id { get; init; }
    public DateTime? Timestamp { get; init; }
}
