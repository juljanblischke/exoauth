using ExoAuth.Api.Filters;
using ExoAuth.Application.Common.Models;
using ExoAuth.Application.Features.Auth.Queries.GetCaptchaConfig;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace ExoAuth.Api.Controllers;

/// <summary>
/// Controller for CAPTCHA configuration.
/// </summary>
[ApiController]
[Route("api/captcha")]
public sealed class CaptchaController : ControllerBase
{
    private readonly IMediator _mediator;

    public CaptchaController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets the public CAPTCHA configuration for the frontend.
    /// </summary>
    /// <remarks>
    /// Returns information about the CAPTCHA provider and site key.
    /// This endpoint is public and rate-limited to prevent enumeration.
    /// </remarks>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The public CAPTCHA configuration.</returns>
    [HttpGet("config")]
    [RateLimit(30)]
    [ProducesResponseType(typeof(ApiResponse<CaptchaPublicConfig>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConfig(CancellationToken ct)
    {
        var query = new GetCaptchaConfigQuery();
        var result = await _mediator.Send(query, ct);
        return Ok(ApiResponse<CaptchaPublicConfig>.Success(result));
    }
}
