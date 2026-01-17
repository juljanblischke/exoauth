using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Queries.GetCaptchaConfig;

/// <summary>
/// Handler for getting the public CAPTCHA configuration.
/// </summary>
public sealed class GetCaptchaConfigHandler : IQueryHandler<GetCaptchaConfigQuery, CaptchaPublicConfig>
{
    private readonly ICaptchaService _captchaService;

    public GetCaptchaConfigHandler(ICaptchaService captchaService)
    {
        _captchaService = captchaService;
    }

    public ValueTask<CaptchaPublicConfig> Handle(GetCaptchaConfigQuery query, CancellationToken ct)
    {
        var config = _captchaService.GetPublicConfig();
        return ValueTask.FromResult(config);
    }
}
