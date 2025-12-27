using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using ExoAuth.Application.Features.SystemUsers.Models;
using Mediator;

namespace ExoAuth.Application.Features.SystemUsers.Queries.GetSystemUsers;

public sealed class GetSystemUsersHandler : IQueryHandler<GetSystemUsersQuery, CursorPagedList<SystemUserDto>>
{
    private readonly ISystemUserRepository _userRepository;

    public GetSystemUsersHandler(ISystemUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async ValueTask<CursorPagedList<SystemUserDto>> Handle(GetSystemUsersQuery query, CancellationToken ct)
    {
        var limit = Math.Clamp(query.Limit, 1, 100);

        var (users, nextCursor, total) = await _userRepository.GetPagedAsync(
            cursor: query.Cursor,
            limit: limit,
            sortBy: query.Sort,
            search: query.Search,
            permissionIds: query.PermissionIds,
            cancellationToken: ct
        );

        var dtos = users.Select(u => new SystemUserDto(
            Id: u.Id,
            Email: u.Email,
            FirstName: u.FirstName,
            LastName: u.LastName,
            FullName: u.FullName,
            IsActive: u.IsActive,
            EmailVerified: u.EmailVerified,
            LastLoginAt: u.LastLoginAt,
            CreatedAt: u.CreatedAt,
            UpdatedAt: u.UpdatedAt
        )).ToList();

        return CursorPagedList<SystemUserDto>.FromItems(
            items: dtos,
            nextCursor: nextCursor,
            hasMore: nextCursor is not null,
            pageSize: limit
        );
    }
}
