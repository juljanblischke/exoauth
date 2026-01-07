using ExoAuth.Application.Common.Models;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Queries.GetCaptchaConfig;

/// <summary>
/// Query to get the public CAPTCHA configuration for the frontend.
/// </summary>
public sealed record GetCaptchaConfigQuery : IQuery<CaptchaPublicConfig>;
