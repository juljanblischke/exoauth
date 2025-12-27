using System.Text;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using ExoAuth.Application.Features.SystemInvites.Models;
using ExoAuth.Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.SystemInvites.Queries.GetSystemInvites;

public sealed class GetSystemInvitesHandler : IQueryHandler<GetSystemInvitesQuery, CursorPagedList<SystemInviteListDto>>
{
    private readonly IAppDbContext _context;

    public GetSystemInvitesHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async ValueTask<CursorPagedList<SystemInviteListDto>> Handle(GetSystemInvitesQuery query, CancellationToken ct)
    {
        var limit = Math.Clamp(query.Limit, 1, 100);

        var dbQuery = _context.SystemInvites
            .Include(i => i.InvitedByUser)
            .AsQueryable();

        // Search by email
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchLower = query.Search.ToLowerInvariant();
            dbQuery = dbQuery.Where(i =>
                i.Email.Contains(searchLower) ||
                i.FirstName.ToLower().Contains(searchLower) ||
                i.LastName.ToLower().Contains(searchLower));
        }

        // We need to materialize to filter by computed Status property
        // First get candidates then filter in memory for status
        var allInvites = await dbQuery
            .OrderByDescending(i => i.CreatedAt)
            .ThenBy(i => i.Id)
            .ToListAsync(ct);

        // Filter by status in memory (Status is computed)
        IEnumerable<SystemInvite> filteredInvites = allInvites;
        if (query.Statuses is { Count: > 0 })
        {
            var statusSet = query.Statuses.Select(s => s.ToLowerInvariant()).ToHashSet();
            filteredInvites = allInvites.Where(i => statusSet.Contains(i.Status));
        }

        var invitesList = filteredInvites.ToList();

        // Apply cursor pagination
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            var cursorData = DecodeCursor(query.Cursor);
            if (cursorData is not null)
            {
                invitesList = invitesList
                    .Where(i => i.CreatedAt < cursorData.Value.createdAt ||
                        (i.CreatedAt == cursorData.Value.createdAt && i.Id.CompareTo(cursorData.Value.id) > 0))
                    .ToList();
            }
        }

        // Take limit + 1 to check for more
        var pagedInvites = invitesList.Take(limit + 1).ToList();

        string? nextCursor = null;
        if (pagedInvites.Count > limit)
        {
            pagedInvites = pagedInvites.Take(limit).ToList();
            var lastInvite = pagedInvites.Last();
            nextCursor = EncodeCursor(lastInvite.CreatedAt, lastInvite.Id);
        }

        var dtos = pagedInvites.Select(i => new SystemInviteListDto(
            Id: i.Id,
            Email: i.Email,
            FirstName: i.FirstName,
            LastName: i.LastName,
            Status: i.Status,
            ExpiresAt: i.ExpiresAt,
            CreatedAt: i.CreatedAt,
            AcceptedAt: i.AcceptedAt,
            RevokedAt: i.RevokedAt,
            ResentAt: i.ResentAt,
            InvitedBy: new InvitedByDto(
                Id: i.InvitedByUser.Id,
                Email: i.InvitedByUser.Email,
                FullName: i.InvitedByUser.FullName
            )
        )).ToList();

        return CursorPagedList<SystemInviteListDto>.FromItems(
            items: dtos,
            nextCursor: nextCursor,
            hasMore: nextCursor is not null,
            pageSize: limit
        );
    }

    private static string EncodeCursor(DateTime createdAt, Guid id)
    {
        var data = $"{createdAt:O}|{id}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(data));
    }

    private static (DateTime createdAt, Guid id)? DecodeCursor(string cursor)
    {
        try
        {
            var data = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var parts = data.Split('|');
            if (parts.Length != 2)
                return null;

            var createdAt = DateTime.Parse(parts[0]);
            var id = Guid.Parse(parts[1]);
            return (createdAt, id);
        }
        catch
        {
            return null;
        }
    }
}
