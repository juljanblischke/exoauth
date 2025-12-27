using System.Text;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Infrastructure.Persistence.Repositories;

public sealed class SystemUserRepository : ISystemUserRepository
{
    private readonly AppDbContext _context;

    public SystemUserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SystemUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SystemUsers
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<SystemUser?> GetByIdWithPermissionsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SystemUsers
            .Include(u => u.Permissions)
                .ThenInclude(p => p.SystemPermission)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<SystemUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToLowerInvariant();
        return await _context.SystemUsers
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
    }

    public async Task<SystemUser?> GetByEmailWithPermissionsAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToLowerInvariant();
        return await _context.SystemUsers
            .Include(u => u.Permissions)
                .ThenInclude(p => p.SystemPermission)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToLowerInvariant();

        // Check SystemUsers
        var existsInSystem = await _context.SystemUsers
            .AnyAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (existsInSystem)
            return true;

        // TODO: Check OrganizationUsers when implemented (Task 003)
        // var existsInOrg = await _context.OrganizationUsers
        //     .AnyAsync(u => u.Email == normalizedEmail, cancellationToken);

        return false;
    }

    public async Task<bool> AnyExistsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SystemUsers.AnyAsync(cancellationToken);
    }

    public async Task<SystemUser> AddAsync(SystemUser user, CancellationToken cancellationToken = default)
    {
        await _context.SystemUsers.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task UpdateAsync(SystemUser user, CancellationToken cancellationToken = default)
    {
        _context.SystemUsers.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(SystemUser user, CancellationToken cancellationToken = default)
    {
        _context.SystemUsers.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<(List<SystemUser> Users, string? NextCursor, int Total)> GetPagedAsync(
        string? cursor,
        int limit,
        string? sortBy,
        string? search,
        List<Guid>? permissionIds = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SystemUsers.AsQueryable();

        // Search
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLowerInvariant();
            query = query.Where(u =>
                u.Email.Contains(searchLower) ||
                u.FirstName.ToLower().Contains(searchLower) ||
                u.LastName.ToLower().Contains(searchLower));
        }

        // Permission filter - users must have ALL specified permissions
        if (permissionIds is { Count: > 0 })
        {
            foreach (var permissionId in permissionIds)
            {
                query = query.Where(u => u.Permissions.Any(p => p.SystemPermissionId == permissionId));
            }
        }

        // Get total count before pagination
        var total = await query.CountAsync(cancellationToken);

        // Sorting - default is CreatedAt descending
        query = ApplySorting(query, sortBy);

        // Cursor-based pagination
        if (!string.IsNullOrWhiteSpace(cursor))
        {
            var cursorData = DecodeCursor(cursor);
            if (cursorData is not null)
            {
                query = query.Where(u => u.CreatedAt < cursorData.Value.createdAt ||
                    (u.CreatedAt == cursorData.Value.createdAt && u.Id.CompareTo(cursorData.Value.id) > 0));
            }
        }

        // Fetch one extra to determine if there are more
        var users = await query
            .Take(limit + 1)
            .ToListAsync(cancellationToken);

        string? nextCursor = null;
        if (users.Count > limit)
        {
            users = users.Take(limit).ToList();
            var lastUser = users.Last();
            nextCursor = EncodeCursor(lastUser.CreatedAt, lastUser.Id);
        }

        return (users, nextCursor, total);
    }

    public async Task<int> CountUsersWithPermissionAsync(string permissionName, CancellationToken cancellationToken = default)
    {
        return await _context.SystemUserPermissions
            .Include(up => up.SystemPermission)
            .Include(up => up.SystemUser)
            .Where(up => up.SystemPermission.Name == permissionName && up.SystemUser.IsActive)
            .CountAsync(cancellationToken);
    }

    public async Task<List<string>> GetUserPermissionNamesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.SystemUserPermissions
            .Include(up => up.SystemPermission)
            .Where(up => up.SystemUserId == userId)
            .Select(up => up.SystemPermission.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task SetUserPermissionsAsync(Guid userId, List<Guid> permissionIds, Guid? grantedBy = null, CancellationToken cancellationToken = default)
    {
        // Remove existing permissions
        var existingPermissions = await _context.SystemUserPermissions
            .Where(up => up.SystemUserId == userId)
            .ToListAsync(cancellationToken);

        _context.SystemUserPermissions.RemoveRange(existingPermissions);

        // Add new permissions
        var newPermissions = permissionIds.Select(permissionId =>
            SystemUserPermission.Create(userId, permissionId, grantedBy)).ToList();

        await _context.SystemUserPermissions.AddRangeAsync(newPermissions, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<SystemUser> ApplySorting(IQueryable<SystemUser> query, string? sortBy)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            return query.OrderByDescending(u => u.CreatedAt).ThenBy(u => u.Id);
        }

        var sortParts = sortBy.Split(',');
        IOrderedQueryable<SystemUser>? orderedQuery = null;

        foreach (var part in sortParts)
        {
            var trimmed = part.Trim();
            var colonIndex = trimmed.IndexOf(':');
            var field = colonIndex > 0 ? trimmed[..colonIndex] : trimmed;
            var direction = colonIndex > 0 ? trimmed[(colonIndex + 1)..] : "asc";
            var isDescending = direction.Equals("desc", StringComparison.OrdinalIgnoreCase);

            orderedQuery = (orderedQuery, field.ToLowerInvariant()) switch
            {
                (null, "email") => isDescending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
                (null, "firstname") => isDescending ? query.OrderByDescending(u => u.FirstName) : query.OrderBy(u => u.FirstName),
                (null, "lastname") => isDescending ? query.OrderByDescending(u => u.LastName) : query.OrderBy(u => u.LastName),
                (null, "createdat") => isDescending ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt),
                (null, "lastloginat") => isDescending ? query.OrderByDescending(u => u.LastLoginAt) : query.OrderBy(u => u.LastLoginAt),
                (null, _) => query.OrderByDescending(u => u.CreatedAt),

                (not null, "email") => isDescending ? orderedQuery.ThenByDescending(u => u.Email) : orderedQuery.ThenBy(u => u.Email),
                (not null, "firstname") => isDescending ? orderedQuery.ThenByDescending(u => u.FirstName) : orderedQuery.ThenBy(u => u.FirstName),
                (not null, "lastname") => isDescending ? orderedQuery.ThenByDescending(u => u.LastName) : orderedQuery.ThenBy(u => u.LastName),
                (not null, "createdat") => isDescending ? orderedQuery.ThenByDescending(u => u.CreatedAt) : orderedQuery.ThenBy(u => u.CreatedAt),
                (not null, "lastloginat") => isDescending ? orderedQuery.ThenByDescending(u => u.LastLoginAt) : orderedQuery.ThenBy(u => u.LastLoginAt),
                (not null, _) => orderedQuery.ThenByDescending(u => u.CreatedAt)
            };
        }

        return orderedQuery ?? query.OrderByDescending(u => u.CreatedAt);
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
