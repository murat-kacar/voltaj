namespace Voltflow.Application.Dtos;

public sealed record LoginRequest(string Email, string Password);
public sealed record RegisterUserRequest(string Name, string Email, string Password, string? Otp = null);
public sealed record AuthResultDto(string UserId, string Name, string Email, string Token, bool IsApproved);
public sealed record AssignRoleRequest(string RoleName);
public sealed record RevokeSessionRequest(string Token);
public sealed record PasswordResetRequest(string Email);
public sealed record CompletePasswordResetRequest(string Token, string NewPassword, string? Email = null);

