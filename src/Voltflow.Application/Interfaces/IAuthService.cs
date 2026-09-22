using Voltflow.Application.Common;
using Voltflow.Application.Dtos;
using Voltflow.Shared;

namespace Voltflow.Application.Interfaces;

public interface IAuthService
{
    /// <param name="approved">Narrows the list to the approved (true) or the waiting (false) users; null lists everyone.</param>
    Task<Result<PagedResult<UserSummaryDto>>> ListUsersAsync(bool? approved = null, int? limit = null, int? offset = null, CancellationToken ct = default);
    Task<Result<UserSummaryDto>> GetUserAsync(Guid userId, CancellationToken ct = default);

    Task<Result<AuthResultDto>> RegisterAsync(RegisterUserRequest request, CancellationToken ct = default);
    Task<Result<AuthResultDto>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<Result<AuthResultDto>> ApproveAsync(Guid userId, CancellationToken ct = default);
    Task<Result> AssignRoleAsync(Guid userId, string roleName, CancellationToken ct = default);
    Task<Result> RevokeSessionAsync(string token, CancellationToken ct = default);
    Task<Result> RequestPasswordResetAsync(string email, CancellationToken ct = default);
    Task<Result> CompletePasswordResetAsync(string token, string newPassword, string? email = null, CancellationToken ct = default);

}
