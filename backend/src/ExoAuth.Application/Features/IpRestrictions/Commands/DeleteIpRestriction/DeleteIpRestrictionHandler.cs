using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.IpRestrictions.Commands.DeleteIpRestriction;

public sealed class DeleteIpRestrictionHandler : ICommandHandler<DeleteIpRestrictionCommand>
{
    private readonly IAppDbContext _dbContext;
    private readonly IIpRestrictionService _ipRestrictionService;

    public DeleteIpRestrictionHandler(
        IAppDbContext dbContext,
        IIpRestrictionService ipRestrictionService)
    {
        _dbContext = dbContext;
        _ipRestrictionService = ipRestrictionService;
    }

    public async ValueTask<Unit> Handle(DeleteIpRestrictionCommand command, CancellationToken ct)
    {
        var restriction = await _dbContext.IpRestrictions
            .FirstOrDefaultAsync(x => x.Id == command.Id, ct);

        if (restriction == null)
        {
            throw new IpRestrictionNotFoundException(command.Id);
        }

        _dbContext.IpRestrictions.Remove(restriction);
        await _dbContext.SaveChangesAsync(ct);

        // Invalidate cache
        await _ipRestrictionService.InvalidateCacheAsync(ct);

        return Unit.Value;
    }
}
