using ExoAuth.Domain.Constants;
using ExoAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Infrastructure.Persistence.Seeders;

public sealed class SystemPermissionSeeder
{
    private readonly AppDbContext _context;
    private readonly ILogger<SystemPermissionSeeder> _logger;

    public SystemPermissionSeeder(AppDbContext context, ILogger<SystemPermissionSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existingPermissions = await _context.SystemPermissions
            .Select(p => p.Name)
            .ToListAsync(cancellationToken);

        var permissionsToAdd = SystemPermissions.All
            .Where(p => !existingPermissions.Contains(p.Name))
            .Select(p => SystemPermission.Create(p.Name, p.Description, p.Category))
            .ToList();

        if (permissionsToAdd.Count > 0)
        {
            await _context.SystemPermissions.AddRangeAsync(permissionsToAdd, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Seeded {Count} system permissions", permissionsToAdd.Count);
        }
        else
        {
            _logger.LogInformation("All system permissions already exist");
        }
    }
}
