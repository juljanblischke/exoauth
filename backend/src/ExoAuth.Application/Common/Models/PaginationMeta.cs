namespace ExoAuth.Application.Common.Models;

public sealed record PaginationMeta
{
    public string? Cursor { get; init; }
    public string? NextCursor { get; init; }
    public bool HasMore { get; init; }
    public int PageSize { get; init; }

    public static PaginationMeta Create(string? cursor, string? nextCursor, bool hasMore, int pageSize)
    {
        return new PaginationMeta
        {
            Cursor = cursor,
            NextCursor = nextCursor,
            HasMore = hasMore,
            PageSize = pageSize
        };
    }

    public static PaginationMeta Empty(int pageSize = 20)
    {
        return new PaginationMeta
        {
            Cursor = null,
            NextCursor = null,
            HasMore = false,
            PageSize = pageSize
        };
    }
}
