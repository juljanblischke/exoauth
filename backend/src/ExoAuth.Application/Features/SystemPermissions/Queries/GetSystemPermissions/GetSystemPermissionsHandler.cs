using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.SystemPermissions.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.SystemPermissions.Queries.GetSystemPermissions;

public sealed class GetSystemPermissionsHandler : IQueryHandler<GetSystemPermissionsQuery, IReadOnlyList<SystemPermissionDto>>
{
    private readonly IAppDbContext _context;

    public GetSystemPermissionsHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async ValueTask<IReadOnlyList<SystemPermissionDto>> Handle(
        GetSystemPermissionsQuery query,
        CancellationToken ct)
    {
        var permissions = await _context.SystemPermissions
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Name)
            .Select(p => new SystemPermissionDto(
                p.Id,
                p.Name,
                p.Description,
                p.Category,
                p.CreatedAt
            ))
            .ToListAsync(ct);

        return permissions;
    }
}
