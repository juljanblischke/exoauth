using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Email.Models;
using ExoAuth.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Email.Logs.Queries.GetEmailLogFilters;

public sealed class GetEmailLogFiltersHandler : IQueryHandler<GetEmailLogFiltersQuery, EmailLogFiltersDto>
{
    private readonly IAppDbContext _dbContext;

    public GetEmailLogFiltersHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<EmailLogFiltersDto> Handle(GetEmailLogFiltersQuery query, CancellationToken ct)
    {
        // Get distinct template names from the database
        var templates = await _dbContext.EmailLogs
            .Select(x => x.TemplateName)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(ct);

        // Get all status options with labels
        var statuses = Enum.GetValues<EmailStatus>()
            .Select(s => new EmailStatusFilterOption(s, GetStatusLabel(s)))
            .ToList();

        return new EmailLogFiltersDto(templates, statuses);
    }

    private static string GetStatusLabel(EmailStatus status) => status switch
    {
        EmailStatus.Queued => "Queued",
        EmailStatus.Sending => "Sending",
        EmailStatus.Sent => "Sent",
        EmailStatus.Failed => "Failed",
        EmailStatus.InDlq => "In DLQ",
        _ => status.ToString()
    };
}
