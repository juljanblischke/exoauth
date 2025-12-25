using ExoAuth.Application.Features.SystemAuditLogs.Models;
using Mediator;

namespace ExoAuth.Application.Features.SystemAuditLogs.Queries.GetAuditLogFilters;

/// <summary>
/// Query to get available filter options for audit logs.
/// </summary>
public sealed record GetAuditLogFiltersQuery() : IQuery<AuditLogFiltersDto>;
