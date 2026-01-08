using ExoAuth.Api.Filters;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using ExoAuth.Application.Features.Email.Configuration.Commands.UpdateEmailConfiguration;
using ExoAuth.Application.Features.Email.Configuration.Queries.GetEmailConfiguration;
using ExoAuth.Application.Features.Email.Dlq.Commands.DeleteDlqEmail;
using ExoAuth.Application.Features.Email.Dlq.Commands.RetryAllDlqEmails;
using ExoAuth.Application.Features.Email.Dlq.Commands.RetryDlqEmail;
using ExoAuth.Application.Features.Email.Dlq.Queries.GetDlqEmails;
using ExoAuth.Application.Features.Email.Logs.Queries.GetEmailLog;
using ExoAuth.Application.Features.Email.Logs.Queries.GetEmailLogFilters;
using ExoAuth.Application.Features.Email.Logs.Queries.GetEmailLogs;
using ExoAuth.Application.Features.Email.Models;
using ExoAuth.Application.Features.Email.Providers.Commands.CreateEmailProvider;
using ExoAuth.Application.Features.Email.Providers.Commands.DeleteEmailProvider;
using ExoAuth.Application.Features.Email.Providers.Commands.ReorderProviders;
using ExoAuth.Application.Features.Email.Providers.Commands.ResetCircuitBreaker;
using ExoAuth.Application.Features.Email.Providers.Commands.UpdateEmailProvider;
using ExoAuth.Application.Features.Email.Providers.Queries.GetEmailProvider;
using ExoAuth.Application.Features.Email.Providers.Queries.GetEmailProviders;
using ExoAuth.Application.Features.Email.Test.Commands.SendTestEmail;
using ExoAuth.Application.Features.Email.Announcements.Commands.CreateEmailAnnouncement;
using ExoAuth.Application.Features.Email.Announcements.Commands.DeleteEmailAnnouncement;
using ExoAuth.Application.Features.Email.Announcements.Commands.SendEmailAnnouncement;
using ExoAuth.Application.Features.Email.Announcements.Commands.UpdateEmailAnnouncement;
using ExoAuth.Application.Features.Email.Announcements.Queries.GetEmailAnnouncement;
using ExoAuth.Application.Features.Email.Announcements.Queries.GetEmailAnnouncements;
using ExoAuth.Application.Features.Email.Announcements.Queries.PreviewEmailAnnouncement;
using ExoAuth.Domain.Constants;
using ExoAuth.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExoAuth.Api.Controllers;

/// <summary>
/// Controller for managing email system configuration, providers, logs, and DLQ.
/// </summary>
[Route("api/system/email")]
[Authorize]
[RateLimit]
public sealed class SystemEmailController : ApiControllerBase
{
    private readonly ICurrentUserService _currentUserService;

    public SystemEmailController(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    #region Providers

    /// <summary>
    /// Get all email providers.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of email providers.</returns>
    [HttpGet("providers")]
    [SystemPermission(SystemPermissions.EmailProvidersRead)]
    [ProducesResponseType(typeof(ApiResponse<List<EmailProviderDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetProviders(CancellationToken ct)
    {
        var query = new GetEmailProvidersQuery();
        var result = await Mediator.Send(query, ct);
        return ApiOk(result);
    }

    /// <summary>
    /// Get a specific email provider by ID.
    /// </summary>
    /// <param name="id">The provider ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The email provider with decrypted configuration.</returns>
    [HttpGet("providers/{id:guid}")]
    [SystemPermission(SystemPermissions.EmailProvidersRead)]
    [ProducesResponseType(typeof(ApiResponse<EmailProviderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetProvider(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var query = new GetEmailProviderQuery(id);
        var result = await Mediator.Send(query, ct);
        return ApiOk(result);
    }

    /// <summary>
    /// Create a new email provider.
    /// </summary>
    /// <param name="request">The provider details.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created email provider.</returns>
    [HttpPost("providers")]
    [SystemPermission(SystemPermissions.EmailProvidersManage)]
    [ProducesResponseType(typeof(ApiResponse<EmailProviderDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateProvider(
        [FromBody] CreateEmailProviderRequest request,
        CancellationToken ct)
    {
        var command = new CreateEmailProviderCommand(
            request.Name,
            request.Type,
            request.Priority,
            request.IsEnabled,
            request.Configuration);

        var result = await Mediator.Send(command, ct);
        return ApiCreated(result);
    }

    /// <summary>
    /// Update an existing email provider.
    /// </summary>
    /// <param name="id">The provider ID.</param>
    /// <param name="request">The updated provider details.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated email provider.</returns>
    [HttpPut("providers/{id:guid}")]
    [SystemPermission(SystemPermissions.EmailProvidersManage)]
    [ProducesResponseType(typeof(ApiResponse<EmailProviderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateProvider(
        [FromRoute] Guid id,
        [FromBody] UpdateEmailProviderRequest request,
        CancellationToken ct)
    {
        var command = new UpdateEmailProviderCommand(
            id,
            request.Name,
            request.Type,
            request.Priority,
            request.IsEnabled,
            request.Configuration);

        var result = await Mediator.Send(command, ct);
        return ApiOk(result);
    }

    /// <summary>
    /// Delete an email provider.
    /// </summary>
    /// <param name="id">The provider ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("providers/{id:guid}")]
    [SystemPermission(SystemPermissions.EmailProvidersManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteProvider(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var command = new DeleteEmailProviderCommand(id);
        await Mediator.Send(command, ct);
        return ApiNoContent();
    }

    /// <summary>
    /// Reset the circuit breaker for an email provider.
    /// </summary>
    /// <param name="id">The provider ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated email provider.</returns>
    [HttpPost("providers/{id:guid}/reset-circuit-breaker")]
    [SystemPermission(SystemPermissions.EmailProvidersManage)]
    [ProducesResponseType(typeof(ApiResponse<EmailProviderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ResetCircuitBreaker(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var command = new ResetCircuitBreakerCommand(id);
        var result = await Mediator.Send(command, ct);
        return ApiOk(result);
    }

    /// <summary>
    /// Reorder email provider priorities.
    /// </summary>
    /// <param name="request">The ordered list of provider IDs.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The reordered list of providers.</returns>
    [HttpPost("providers/reorder")]
    [SystemPermission(SystemPermissions.EmailProvidersManage)]
    [ProducesResponseType(typeof(ApiResponse<List<EmailProviderDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ReorderProviders(
        [FromBody] ReorderProvidersRequest request,
        CancellationToken ct)
    {
        var command = new ReorderProvidersCommand(request.Providers);
        var result = await Mediator.Send(command, ct);
        return ApiOk(result);
    }

    #endregion

    #region Configuration

    /// <summary>
    /// Get email system configuration.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The email configuration.</returns>
    [HttpGet("configuration")]
    [SystemPermission(SystemPermissions.EmailConfigRead)]
    [ProducesResponseType(typeof(ApiResponse<EmailConfigurationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetConfiguration(CancellationToken ct)
    {
        var query = new GetEmailConfigurationQuery();
        var result = await Mediator.Send(query, ct);
        return ApiOk(result);
    }

    /// <summary>
    /// Update email system configuration.
    /// </summary>
    /// <param name="request">The updated configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated configuration.</returns>
    [HttpPut("configuration")]
    [SystemPermission(SystemPermissions.EmailConfigManage)]
    [ProducesResponseType(typeof(ApiResponse<EmailConfigurationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateConfiguration(
        [FromBody] UpdateEmailConfigurationRequest request,
        CancellationToken ct)
    {
        var command = new UpdateEmailConfigurationCommand(
            request.MaxRetriesPerProvider,
            request.InitialRetryDelayMs,
            request.MaxRetryDelayMs,
            request.BackoffMultiplier,
            request.CircuitBreakerFailureThreshold,
            request.CircuitBreakerWindowMinutes,
            request.CircuitBreakerOpenDurationMinutes,
            request.AutoRetryDlq,
            request.DlqRetryIntervalHours,
            request.EmailsEnabled,
            request.TestMode);

        var result = await Mediator.Send(command, ct);
        return ApiOk(result);
    }

    #endregion

    #region Logs

    /// <summary>
    /// Get paginated email logs.
    /// </summary>
    /// <param name="cursor">Pagination cursor.</param>
    /// <param name="limit">Number of items per page (default 20).</param>
    /// <param name="status">Filter by status.</param>
    /// <param name="templateName">Filter by template name.</param>
    /// <param name="search">Search in recipient email.</param>
    /// <param name="recipientUserId">Filter by recipient user ID.</param>
    /// <param name="announcementId">Filter by announcement ID.</param>
    /// <param name="fromDate">Filter from date.</param>
    /// <param name="toDate">Filter to date.</param>
    /// <param name="sort">Sort field and direction (e.g., "createdAt:desc").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated list of email logs.</returns>
    [HttpGet("logs")]
    [SystemPermission(SystemPermissions.EmailLogsRead)]
    [ProducesResponseType(typeof(ApiResponse<CursorPagedList<EmailLogDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetLogs(
        [FromQuery] string? cursor = null,
        [FromQuery] int limit = 20,
        [FromQuery] EmailStatus? status = null,
        [FromQuery] string? templateName = null,
        [FromQuery] string? search = null,
        [FromQuery] Guid? recipientUserId = null,
        [FromQuery] Guid? announcementId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string sort = "createdAt:desc",
        CancellationToken ct = default)
    {
        var query = new GetEmailLogsQuery(
            cursor,
            limit,
            status,
            templateName,
            search,
            recipientUserId,
            announcementId,
            fromDate,
            toDate,
            sort);

        var result = await Mediator.Send(query, ct);
        return ApiOk(result.Items, result.Pagination);
    }

    /// <summary>
    /// Get a specific email log by ID.
    /// </summary>
    /// <param name="id">The email log ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The email log details.</returns>
    [HttpGet("logs/{id:guid}")]
    [SystemPermission(SystemPermissions.EmailLogsRead)]
    [ProducesResponseType(typeof(ApiResponse<EmailLogDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetLog(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var query = new GetEmailLogQuery(id);
        var result = await Mediator.Send(query, ct);
        return ApiOk(result);
    }

    /// <summary>
    /// Get available filter options for email logs.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Available filter options.</returns>
    [HttpGet("logs/filters")]
    [SystemPermission(SystemPermissions.EmailLogsRead)]
    [ProducesResponseType(typeof(ApiResponse<EmailLogFiltersDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetLogFilters(CancellationToken ct)
    {
        var query = new GetEmailLogFiltersQuery();
        var result = await Mediator.Send(query, ct);
        return ApiOk(result);
    }

    #endregion

    #region DLQ

    /// <summary>
    /// Get paginated emails in the dead letter queue.
    /// </summary>
    /// <param name="cursor">Pagination cursor.</param>
    /// <param name="limit">Number of items per page (default 20).</param>
    /// <param name="search">Search in recipient email.</param>
    /// <param name="sort">Sort field and direction (e.g., "movedToDlqAt:desc").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated list of DLQ emails.</returns>
    [HttpGet("dlq")]
    [SystemPermission(SystemPermissions.EmailDlqManage)]
    [ProducesResponseType(typeof(ApiResponse<CursorPagedList<EmailLogDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDlqEmails(
        [FromQuery] string? cursor = null,
        [FromQuery] int limit = 20,
        [FromQuery] string? search = null,
        [FromQuery] string sort = "movedToDlqAt:desc",
        CancellationToken ct = default)
    {
        var query = new GetDlqEmailsQuery(cursor, limit, search, sort);
        var result = await Mediator.Send(query, ct);
        return ApiOk(result.Items, result.Pagination);
    }

    /// <summary>
    /// Retry sending a specific email from the DLQ.
    /// </summary>
    /// <param name="id">The email log ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated email log.</returns>
    [HttpPost("dlq/{id:guid}/retry")]
    [SystemPermission(SystemPermissions.EmailDlqManage)]
    [ProducesResponseType(typeof(ApiResponse<EmailLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RetryDlqEmail(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var command = new RetryDlqEmailCommand(id);
        var result = await Mediator.Send(command, ct);
        return ApiOk(result);
    }

    /// <summary>
    /// Retry all emails in the DLQ.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Summary of the retry operation.</returns>
    [HttpPost("dlq/retry-all")]
    [SystemPermission(SystemPermissions.EmailDlqManage)]
    [ProducesResponseType(typeof(ApiResponse<RetryAllDlqEmailsResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RetryAllDlqEmails(CancellationToken ct)
    {
        var command = new RetryAllDlqEmailsCommand();
        var result = await Mediator.Send(command, ct);
        return ApiOk(result);
    }

    /// <summary>
    /// Delete an email from the DLQ.
    /// </summary>
    /// <param name="id">The email log ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("dlq/{id:guid}")]
    [SystemPermission(SystemPermissions.EmailDlqManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteDlqEmail(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var command = new DeleteDlqEmailCommand(id);
        await Mediator.Send(command, ct);
        return ApiNoContent();
    }

    #endregion

    #region Test

    /// <summary>
    /// Send a test email to verify provider configuration.
    /// </summary>
    /// <param name="request">The test email request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The test result.</returns>
    [HttpPost("test")]
    [SystemPermission(SystemPermissions.EmailTest)]
    [ProducesResponseType(typeof(ApiResponse<TestEmailResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SendTestEmail(
        [FromBody] SendTestEmailRequest request,
        CancellationToken ct)
    {
        var command = new SendTestEmailCommand(request.RecipientEmail, request.ProviderId);
        var result = await Mediator.Send(command, ct);
        return ApiOk(result);
    }

    #endregion

    #region Announcements

    /// <summary>
    /// Get paginated email announcements.
    /// </summary>
    /// <param name="cursor">Pagination cursor.</param>
    /// <param name="limit">Number of items per page (default 20).</param>
    /// <param name="status">Filter by status.</param>
    /// <param name="search">Search in subject.</param>
    /// <param name="sort">Sort field and direction (e.g., "createdAt:desc").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated list of announcements.</returns>
    [HttpGet("announcements")]
    [SystemPermission(SystemPermissions.EmailAnnouncementsRead)]
    [ProducesResponseType(typeof(ApiResponse<CursorPagedList<EmailAnnouncementDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAnnouncements(
        [FromQuery] string? cursor = null,
        [FromQuery] int limit = 20,
        [FromQuery] EmailAnnouncementStatus? status = null,
        [FromQuery] string? search = null,
        [FromQuery] string sort = "createdAt:desc",
        CancellationToken ct = default)
    {
        var query = new GetEmailAnnouncementsQuery(cursor, limit, status, search, sort);
        var result = await Mediator.Send(query, ct);
        return ApiOk(result.Items, result.Pagination);
    }

    /// <summary>
    /// Get a specific email announcement by ID.
    /// </summary>
    /// <param name="id">The announcement ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The announcement details.</returns>
    [HttpGet("announcements/{id:guid}")]
    [SystemPermission(SystemPermissions.EmailAnnouncementsRead)]
    [ProducesResponseType(typeof(ApiResponse<EmailAnnouncementDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAnnouncement(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var query = new GetEmailAnnouncementQuery(id);
        var result = await Mediator.Send(query, ct);
        return ApiOk(result);
    }

    /// <summary>
    /// Create a new email announcement.
    /// </summary>
    /// <param name="request">The announcement details.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created announcement.</returns>
    [HttpPost("announcements")]
    [SystemPermission(SystemPermissions.EmailAnnouncementsManage)]
    [ProducesResponseType(typeof(ApiResponse<EmailAnnouncementDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateAnnouncement(
        [FromBody] CreateAnnouncementRequest request,
        CancellationToken ct)
    {
        var command = new CreateEmailAnnouncementCommand(
            request.Subject,
            request.HtmlBody,
            request.PlainTextBody,
            request.TargetType,
            request.TargetPermission,
            request.TargetUserIds,
            _currentUserService.UserId!.Value);

        var result = await Mediator.Send(command, ct);
        return ApiCreated(result);
    }

    /// <summary>
    /// Update an existing email announcement (draft only).
    /// </summary>
    /// <param name="id">The announcement ID.</param>
    /// <param name="request">The updated announcement details.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated announcement.</returns>
    [HttpPut("announcements/{id:guid}")]
    [SystemPermission(SystemPermissions.EmailAnnouncementsManage)]
    [ProducesResponseType(typeof(ApiResponse<EmailAnnouncementDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateAnnouncement(
        [FromRoute] Guid id,
        [FromBody] UpdateAnnouncementRequest request,
        CancellationToken ct)
    {
        var command = new UpdateEmailAnnouncementCommand(
            id,
            request.Subject,
            request.HtmlBody,
            request.PlainTextBody,
            request.TargetType,
            request.TargetPermission,
            request.TargetUserIds);

        var result = await Mediator.Send(command, ct);
        return ApiOk(result);
    }

    /// <summary>
    /// Delete an email announcement (draft only).
    /// </summary>
    /// <param name="id">The announcement ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("announcements/{id:guid}")]
    [SystemPermission(SystemPermissions.EmailAnnouncementsManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteAnnouncement(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var command = new DeleteEmailAnnouncementCommand(id);
        await Mediator.Send(command, ct);
        return ApiNoContent();
    }

    /// <summary>
    /// Send an email announcement to all recipients.
    /// </summary>
    /// <param name="id">The announcement ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The announcement with updated status.</returns>
    [HttpPost("announcements/{id:guid}/send")]
    [SystemPermission(SystemPermissions.EmailAnnouncementsManage)]
    [ProducesResponseType(typeof(ApiResponse<EmailAnnouncementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SendAnnouncement(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var command = new SendEmailAnnouncementCommand(id);
        var result = await Mediator.Send(command, ct);
        return ApiOk(result);
    }

    /// <summary>
    /// Preview an announcement and get estimated recipients.
    /// </summary>
    /// <param name="request">The announcement preview request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The preview with estimated recipients.</returns>
    [HttpPost("announcements/preview")]
    [SystemPermission(SystemPermissions.EmailAnnouncementsManage)]
    [ProducesResponseType(typeof(ApiResponse<AnnouncementPreviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> PreviewAnnouncement(
        [FromBody] PreviewAnnouncementRequest request,
        CancellationToken ct)
    {
        var query = new PreviewEmailAnnouncementQuery(
            request.Subject,
            request.HtmlBody,
            request.PlainTextBody,
            EmailAnnouncementTarget.AllUsers,
            null,
            null);

        var result = await Mediator.Send(query, ct);
        return ApiOk(result);
    }

    #endregion
}
