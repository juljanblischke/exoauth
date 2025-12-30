using System.Linq.Expressions;
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
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetSystemInvitesHandler(IAppDbContext context, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async ValueTask<CursorPagedList<SystemInviteListDto>> Handle(GetSystemInvitesQuery query, CancellationToken ct)
    {
        var limit = Math.Clamp(query.Limit, 1, 100);
        var now = _dateTimeProvider.UtcNow;

        var dbQuery = _context.SystemInvites
            .Include(i => i.InvitedByUser)
            .AsQueryable();

        // Search by email, firstName, lastName
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchLower = query.Search.ToLowerInvariant();
            dbQuery = dbQuery.Where(i =>
                i.Email.Contains(searchLower) ||
                i.FirstName.ToLower().Contains(searchLower) ||
                i.LastName.ToLower().Contains(searchLower));
        }

        // Apply SQL-based status filtering
        dbQuery = ApplyStatusFilter(dbQuery, query.Statuses, query.IncludeExpired, query.IncludeRevoked, now);

        // Parse sort options
        var (sortField, sortDescending) = ParseSort(query.Sort);

        // Apply cursor pagination (before ordering to use index efficiently)
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            var cursorData = DecodeCursor(query.Cursor);
            if (cursorData is not null)
            {
                dbQuery = ApplyCursorFilter(dbQuery, cursorData.Value, sortField, sortDescending);
            }
        }

        // Apply ordering based on sort field
        dbQuery = ApplySorting(dbQuery, sortField, sortDescending);

        // Take limit + 1 to check for more
        var invites = await dbQuery
            .Take(limit + 1)
            .ToListAsync(ct);

        string? nextCursor = null;
        if (invites.Count > limit)
        {
            invites = invites.Take(limit).ToList();
            var lastInvite = invites.Last();
            nextCursor = EncodeCursor(lastInvite, sortField);
        }

        var dtos = invites.Select(i => new SystemInviteListDto(
            Id: i.Id,
            Email: i.Email,
            FirstName: i.FirstName,
            LastName: i.LastName,
            Status: GetStatus(i, now),
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

    private static IQueryable<SystemInvite> ApplyStatusFilter(
        IQueryable<SystemInvite> query,
        List<string>? statuses,
        bool includeExpired,
        bool includeRevoked,
        DateTime now)
    {
        // If specific statuses are provided, use them
        if (statuses is { Count: > 0 })
        {
            var statusSet = statuses.Select(s => s.ToLowerInvariant()).ToHashSet();

            // Build OR conditions for each status
            Expression<Func<SystemInvite, bool>>? predicate = null;

            foreach (var status in statusSet)
            {
                Expression<Func<SystemInvite, bool>> statusExpr = status switch
                {
                    "pending" => i => i.AcceptedAt == null && i.RevokedAt == null && i.ExpiresAt > now,
                    "accepted" => i => i.AcceptedAt != null,
                    "revoked" => i => i.RevokedAt != null,
                    "expired" => i => i.AcceptedAt == null && i.RevokedAt == null && i.ExpiresAt <= now,
                    _ => i => false
                };

                predicate = predicate == null ? statusExpr : OrExpression(predicate, statusExpr);
            }

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            return query;
        }

        // Default behavior: show pending and accepted, hide expired and revoked unless explicitly included
        if (!includeExpired && !includeRevoked)
        {
            // Show only pending and accepted
            query = query.Where(i =>
                // Pending: not accepted, not revoked, not expired
                (i.AcceptedAt == null && i.RevokedAt == null && i.ExpiresAt > now) ||
                // Accepted
                i.AcceptedAt != null);
        }
        else if (!includeExpired)
        {
            // Show pending, accepted, revoked - hide expired
            query = query.Where(i =>
                // Not expired (either accepted, revoked, or still valid)
                i.AcceptedAt != null || i.RevokedAt != null || i.ExpiresAt > now);
        }
        else if (!includeRevoked)
        {
            // Show pending, accepted, expired - hide revoked
            query = query.Where(i => i.RevokedAt == null);
        }
        // If both includeExpired and includeRevoked are true, show everything

        return query;
    }

    private static Expression<Func<T, bool>> OrExpression<T>(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(T));
        var body = Expression.OrElse(
            Expression.Invoke(left, parameter),
            Expression.Invoke(right, parameter));
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    private static (string field, bool descending) ParseSort(string sort)
    {
        var parts = sort.Split(':');
        var field = parts[0].ToLowerInvariant();
        var direction = parts.Length > 1 ? parts[1].ToLowerInvariant() : "desc";

        // Validate field
        var validFields = new HashSet<string> { "email", "firstname", "lastname", "createdat", "expiresat" };
        if (!validFields.Contains(field))
        {
            field = "createdat";
        }

        return (field, direction == "desc");
    }

    private static IQueryable<SystemInvite> ApplySorting(
        IQueryable<SystemInvite> query,
        string sortField,
        bool descending)
    {
        return sortField switch
        {
            "email" => descending
                ? query.OrderByDescending(i => i.Email).ThenBy(i => i.Id)
                : query.OrderBy(i => i.Email).ThenBy(i => i.Id),
            "firstname" => descending
                ? query.OrderByDescending(i => i.FirstName).ThenBy(i => i.Id)
                : query.OrderBy(i => i.FirstName).ThenBy(i => i.Id),
            "lastname" => descending
                ? query.OrderByDescending(i => i.LastName).ThenBy(i => i.Id)
                : query.OrderBy(i => i.LastName).ThenBy(i => i.Id),
            "expiresat" => descending
                ? query.OrderByDescending(i => i.ExpiresAt).ThenBy(i => i.Id)
                : query.OrderBy(i => i.ExpiresAt).ThenBy(i => i.Id),
            _ => descending // createdat (default)
                ? query.OrderByDescending(i => i.CreatedAt).ThenBy(i => i.Id)
                : query.OrderBy(i => i.CreatedAt).ThenBy(i => i.Id)
        };
    }

    private static IQueryable<SystemInvite> ApplyCursorFilter(
        IQueryable<SystemInvite> query,
        (string value, Guid id) cursor,
        string sortField,
        bool descending)
    {
        var cursorValue = cursor.value;
        var cursorId = cursor.id;

        // For date fields, parse the cursor value
        if (sortField is "createdat" or "expiresat")
        {
            if (!DateTime.TryParse(cursorValue, out var cursorDate))
            {
                return query;
            }

            return sortField switch
            {
                "createdat" => descending
                    ? query.Where(i => i.CreatedAt < cursorDate || (i.CreatedAt == cursorDate && i.Id.CompareTo(cursorId) > 0))
                    : query.Where(i => i.CreatedAt > cursorDate || (i.CreatedAt == cursorDate && i.Id.CompareTo(cursorId) > 0)),
                "expiresat" => descending
                    ? query.Where(i => i.ExpiresAt < cursorDate || (i.ExpiresAt == cursorDate && i.Id.CompareTo(cursorId) > 0))
                    : query.Where(i => i.ExpiresAt > cursorDate || (i.ExpiresAt == cursorDate && i.Id.CompareTo(cursorId) > 0)),
                _ => query
            };
        }

        // For string fields
        return sortField switch
        {
            "email" => descending
                ? query.Where(i => string.Compare(i.Email, cursorValue) < 0 || (i.Email == cursorValue && i.Id.CompareTo(cursorId) > 0))
                : query.Where(i => string.Compare(i.Email, cursorValue) > 0 || (i.Email == cursorValue && i.Id.CompareTo(cursorId) > 0)),
            "firstname" => descending
                ? query.Where(i => string.Compare(i.FirstName, cursorValue) < 0 || (i.FirstName == cursorValue && i.Id.CompareTo(cursorId) > 0))
                : query.Where(i => string.Compare(i.FirstName, cursorValue) > 0 || (i.FirstName == cursorValue && i.Id.CompareTo(cursorId) > 0)),
            "lastname" => descending
                ? query.Where(i => string.Compare(i.LastName, cursorValue) < 0 || (i.LastName == cursorValue && i.Id.CompareTo(cursorId) > 0))
                : query.Where(i => string.Compare(i.LastName, cursorValue) > 0 || (i.LastName == cursorValue && i.Id.CompareTo(cursorId) > 0)),
            _ => query
        };
    }

    private static string GetStatus(SystemInvite invite, DateTime now)
    {
        if (invite.RevokedAt.HasValue) return "revoked";
        if (invite.AcceptedAt.HasValue) return "accepted";
        if (now > invite.ExpiresAt) return "expired";
        return "pending";
    }

    private static string EncodeCursor(SystemInvite invite, string sortField)
    {
        var value = sortField switch
        {
            "email" => invite.Email,
            "firstname" => invite.FirstName,
            "lastname" => invite.LastName,
            "expiresat" => invite.ExpiresAt.ToString("O"),
            _ => invite.CreatedAt.ToString("O")
        };

        var data = $"{value}|{invite.Id}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(data));
    }

    private static (string value, Guid id)? DecodeCursor(string cursor)
    {
        try
        {
            var data = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var parts = data.Split('|');
            if (parts.Length != 2)
                return null;

            var value = parts[0];
            var id = Guid.Parse(parts[1]);
            return (value, id);
        }
        catch
        {
            return null;
        }
    }
}
