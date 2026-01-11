using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Email.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Email.Logs.Queries.GetEmailLog;

public sealed class GetEmailLogHandler : IQueryHandler<GetEmailLogQuery, EmailLogDetailDto>
{
    private readonly IAppDbContext _dbContext;

    public GetEmailLogHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<EmailLogDetailDto> Handle(GetEmailLogQuery query, CancellationToken ct)
    {
        var emailLog = await _dbContext.EmailLogs
            .Include(x => x.RecipientUser)
            .Include(x => x.SentViaProvider)
            .FirstOrDefaultAsync(x => x.Id == query.Id, ct);

        if (emailLog is null)
        {
            throw new EmailLogNotFoundException(query.Id);
        }

        return new EmailLogDetailDto(
            emailLog.Id,
            emailLog.RecipientUserId,
            emailLog.RecipientEmail,
            emailLog.RecipientUser != null ? emailLog.RecipientUser.FirstName + " " + emailLog.RecipientUser.LastName : null,
            emailLog.Subject,
            emailLog.TemplateName,
            emailLog.TemplateVariables,
            emailLog.Language,
            emailLog.Status,
            emailLog.RetryCount,
            emailLog.LastError,
            emailLog.SentViaProviderId,
            emailLog.SentViaProvider?.Name,
            emailLog.QueuedAt,
            emailLog.SentAt,
            emailLog.FailedAt,
            emailLog.MovedToDlqAt,
            emailLog.AnnouncementId,
            emailLog.CreatedAt);
    }
}
