using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Identity;
using Voltflow.Shared;
using System.Security.Cryptography;

namespace Voltflow.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IAppUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IUserRoleRepository _userRoles;
    private readonly IPasswordHasher<AppUser> _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IUserSessionRepository _sessions;
    private readonly IPasswordResetTokenRepository _resetTokens;
    private readonly IHostEnvironment _environment;
    private readonly ICommandJournal _commandJournal;
    private readonly IOperationContext _operationContext;
    private readonly ISessionCacheService? _sessionCache;

    public AuthService(
        IAppUserRepository users,
        IRoleRepository roles,
        IUserRoleRepository userRoles,
        IPasswordHasher<AppUser> passwordHasher,
        ITokenService tokenService,
        IUserSessionRepository sessions,
        IPasswordResetTokenRepository resetTokens,
        IHostEnvironment environment,
        ICommandJournal commandJournal,
        IOperationContext operationContext,
        ISessionCacheService? sessionCache = null)
    {
        _users = users;
        _roles = roles;
        _userRoles = userRoles;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _sessions = sessions;
        _resetTokens = resetTokens;
        _environment = environment;
        _commandJournal = commandJournal;
        _operationContext = operationContext;
        _sessionCache = sessionCache;
    }

    public async Task<Result<AuthResultDto>> RegisterAsync(RegisterUserRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return Result<AuthResultDto>.Fail("Name is required.");
        if (string.IsNullOrWhiteSpace(request.Email)) return Result<AuthResultDto>.Fail("Email is required.");
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            return Result<AuthResultDto>.Fail("Password must contain at least 8 characters.");

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _users.GetByEmailAsync(email, ct) is not null)
            return Result<AuthResultDto>.Fail("A user with this email already exists.");

        var user = new AppUser(request.Name.Trim(), email);
        user.SetPasswordHash(_passwordHasher.HashPassword(user, request.Password));

        var roles = new List<string>();
        // Dev-only shortcut for local/QA account setup; never available outside Development (was
        // previously unconditional - CWE-798 hardcoded credential / approval-bypass).
        if (_environment.IsDevelopment() && request.Otp?.Trim() == "000000")
        {
            user.SetVerified();
            user.Approve();
        }

        _commandJournal.MarkResolved(_operationContext.OperationId, success: true, errorCode: null);
        await _users.AddAsync(user, ct);

        if (user.IsApproved)
        {
            var role = await _roles.GetByNameAsync("Technician", ct) ?? await _roles.GetByNameAsync("Viewer", ct);
            if (role is not null)
            {
                await _userRoles.AddAsync(new AppUserRole(user.Id, role.Id), ct);
                roles.Add(role.Name);
            }
        }

        return Result<AuthResultDto>.Ok(Map(user, roles));
    }


    public async Task<Result<AuthResultDto>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return Result<AuthResultDto>.Fail("Email and password are required.");

        var user = await _users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), ct);
        if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash))
            return Result<AuthResultDto>.Fail("Invalid credentials.");

        if (!user.IsApproved)
            return Result<AuthResultDto>.Fail("Account approval is pending.");

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
            return Result<AuthResultDto>.Fail("Invalid credentials.");

        var roles = await _roles.GetNamesByUserAsync(user.Id, ct);
        var token = _tokenService.CreateToken(user, roles);
        await _sessions.AddAsync(new UserSession(user.Id, token, DateTime.UtcNow.AddMinutes(60)), ct);
        return Result<AuthResultDto>.Ok(new AuthResultDto(user.Id.ToString(), user.Name, user.Email, token, user.IsApproved));
    }

    public async Task<Result<AuthResultDto>> ApproveAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return Result<AuthResultDto>.Fail("User not found.");
        if (user.IsApproved) return Result<AuthResultDto>.Fail("User is already approved.");

        var viewerRole = await _roles.GetByNameAsync("Viewer", ct);
        if (viewerRole is null) return Result<AuthResultDto>.Fail("Viewer role is not configured.");

        user.Approve();
        _commandJournal.MarkResolved(_operationContext.OperationId, success: true, errorCode: null);
        await _users.UpdateAsync(user, ct);
        if (!await _userRoles.ExistsAsync(user.Id, viewerRole.Id, ct))
            await _userRoles.AddAsync(new AppUserRole(user.Id, viewerRole.Id), ct);

        return Result<AuthResultDto>.Ok(Map(user, ["Viewer"]));
    }

    public async Task<Result> AssignRoleAsync(Guid userId, string roleName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(roleName)) return Result.Fail("RoleName is required.");
        if (await _users.GetByIdAsync(userId, ct) is null) return Result.Fail("User not found.");

        var role = await _roles.GetByNameAsync(roleName.Trim(), ct);
        if (role is null) return Result.Fail("Role not found.");
        if (await _userRoles.ExistsAsync(userId, role.Id, ct)) return Result.Ok();

        await _userRoles.AddAsync(new AppUserRole(userId, role.Id), ct);
        await _commandJournal.ResolveNowAsync(_operationContext.OperationId, success: true, errorCode: null, ct);
        return Result.Ok();
    }

    public async Task<Result> RevokeSessionAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return Result.Fail("Token is required.");
        var session = await _sessions.GetByTokenAsync(token, ct);
        if (session is null) return Result.Ok();
        session.Revoke();
        await _sessions.UpdateAsync(session, ct);
        await _commandJournal.ResolveNowAsync(_operationContext.OperationId, success: true, errorCode: null, ct);
        if (_sessionCache is not null)
        {
            await _sessionCache.InvalidateSessionAsync(token, ct: ct);
        }
        return Result.Ok();
    }

    public async Task<Result> RequestPasswordResetAsync(string email, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email)) return Result.Fail("Email is required.");
        var user = await _users.GetByEmailAsync(email.Trim().ToLowerInvariant(), ct);
        if (user is null) return Result.Ok();

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        await _resetTokens.AddAsync(new PasswordResetToken(user.Id, token, DateTime.UtcNow.AddMinutes(30)), ct);
        return Result.Ok();
    }

    public async Task<Result> CompletePasswordResetAsync(string token, string newPassword, string? email = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return Result.Fail("Token is required.");
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
            return Result.Fail("Password must contain at least 8 characters.");

        AppUser? user = null;
        // Dev-only shortcut, see RegisterAsync; outside Development this falls through to the
        // real reset-token lookup below, where the literal string "000000" will never match a
        // real 32-byte random token.
        if (_environment.IsDevelopment() && token.Trim() == "000000")
        {
            if (string.IsNullOrWhiteSpace(email))
                return Result.Fail("Email is required when using OTP reset.");

            user = await _users.GetByEmailAsync(email.Trim().ToLowerInvariant(), ct);
            if (user is null) return Result.Fail("User not found.");
        }
        else
        {
            var reset = await _resetTokens.GetAsync(token, ct);
            if (reset is null || !reset.IsUsable) return Result.Fail("Reset token is invalid or expired.");
            user = await _users.GetByIdAsync(reset.UserId, ct);
            if (user is null) return Result.Fail("User not found.");
            reset.MarkUsed();
            await _resetTokens.UpdateAsync(reset, ct);
        }

        user.SetPasswordHash(_passwordHasher.HashPassword(user, newPassword));
        user.SetVerified();
        user.Approve();
        _commandJournal.MarkResolved(_operationContext.OperationId, success: true, errorCode: null);
        await _users.UpdateAsync(user, ct);
        return Result.Ok();
    }


    private AuthResultDto Map(AppUser user, IReadOnlyCollection<string> roles) =>
        new(user.Id.ToString(), user.Name, user.Email,
            user.IsApproved ? _tokenService.CreateToken(user, roles) : string.Empty,
            user.IsApproved);
}
