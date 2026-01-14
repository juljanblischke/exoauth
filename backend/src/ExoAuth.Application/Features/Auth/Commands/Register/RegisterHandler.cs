using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Models;
using ExoAuth.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Auth.Commands.Register;

public sealed class RegisterHandler : ICommandHandler<RegisterCommand, AuthResponse>
{
    private readonly IAppDbContext _context;
    private readonly ISystemUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IMfaService _mfaService;
    private readonly IAuditService _auditService;
    private readonly ICaptchaService _captchaService;

    public RegisterHandler(
        IAppDbContext context,
        ISystemUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IMfaService mfaService,
        IAuditService auditService,
        ICaptchaService captchaService)
    {
        _context = context;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _mfaService = mfaService;
        _auditService = auditService;
        _captchaService = captchaService;
    }

    public async ValueTask<AuthResponse> Handle(RegisterCommand command, CancellationToken ct)
    {
        // Validate CAPTCHA (always required for registration)
        await _captchaService.ValidateRequiredAsync(
            command.CaptchaToken,
            "register",
            command.IpAddress,
            ct);
        // Check if any system users exist
        var anyUsersExist = await _userRepository.AnyExistsAsync(ct);

        if (anyUsersExist)
        {
            // Registration closed for additional system users
            // Later: check for organizationName and create org user
            throw new RegistrationClosedException();
        }

        // Check if email already exists
        if (await _userRepository.EmailExistsAsync(command.Email, ct))
        {
            throw new EmailExistsException();
        }

        // Create user
        var passwordHash = _passwordHasher.Hash(command.Password);
        var user = global::ExoAuth.Domain.Entities.SystemUser.Create(
            email: command.Email,
            passwordHash: passwordHash,
            firstName: command.FirstName,
            lastName: command.LastName,
            emailVerified: true // First user is auto-verified
        );

        // Set preferred language
        user.SetPreferredLanguage(command.Language);

        // Ensure unique ID (handles extremely rare GUID collision)
        const int maxRetries = 3;
        for (var i = 0; i < maxRetries; i++)
        {
            if (!await _context.SystemUsers.AnyAsync(u => u.Id == user.Id, ct))
                break;
            user.RegenerateId();
            if (i == maxRetries - 1)
                throw new InvalidOperationException("Failed to generate unique user ID after multiple attempts");
        }

        await _userRepository.AddAsync(user, ct);

        // Get all system permissions and assign to first user
        var allPermissions = await _context.SystemPermissions
            .Select(p => p.Id)
            .ToListAsync(ct);

        await _userRepository.SetUserPermissionsAsync(user.Id, allPermissions, null, ct);

        // Audit log
        await _auditService.LogWithContextAsync(
            AuditActions.UserRegistered,
            user.Id,
            null, // targetUserId
            "SystemUser",
            user.Id,
            new { user.Email, IsFirstUser = true },
            ct
        );

        // First user has system permissions - require MFA setup before granting access
        // Generate setup token for forced MFA flow
        var setupToken = _mfaService.GenerateMfaToken(user.Id, null);

        // Audit log for MFA setup required
        await _auditService.LogWithContextAsync(
            AuditActions.MfaSetupRequiredSent,
            user.Id,
            null,
            "SystemUser",
            user.Id,
            new { Reason = "FirstUserRegistration" },
            ct
        );

        return AuthResponse.RequiresMfaSetup(setupToken);
    }
}
