using Voltflow.Application.Dtos;
using Voltflow.Shared;

namespace Voltflow.Application.Interfaces;

public interface IAuthService
{
    Task<Result<AuthResultDto>> RegisterAsync(RegisterUserRequest request, CancellationToken ct = default);
    Task<Result<AuthResultDto>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<Result<AuthResultDto>> ApproveAsync(Guid userId, CancellationToken ct = default);
    Task<Result> AssignRoleAsync(Guid userId, string roleName, CancellationToken ct = default);
    Task<Result> RevokeSessionAsync(string token, CancellationToken ct = default);
    Task<Result> RequestPasswordResetAsync(string email, CancellationToken ct = default);
    Task<Result> CompletePasswordResetAsync(string token, string newPassword, string? email = null, CancellationToken ct = default);

}
