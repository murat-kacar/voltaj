using Voltflow.Api.Errors;
using Voltflow.Api.Pagination;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Api.Security;
using Voltflow.Domain.Common;

namespace Voltflow.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        var auth = routes.MapGroup("/api/auth").WithTags("01-IdentityAndAuth");

        auth.MapPost("/register", async (RegisterUserRequest request, IAuthService service, CancellationToken ct) =>
        {
            var result = await service.RegisterAsync(request, ct);
            return result.IsSuccess
                ? Results.Accepted(value: result.Value)
                : result.From();
        })
        .WithName("VF-01201_Register")
        .UseMutationPolicy(RateLimitScope.Strict);

        auth.MapPost("/login", async (LoginRequest request, IAuthService service, CancellationToken ct) =>
        {
            var result = await service.LoginAsync(request, ct);
            if (result.IsSuccess)
                return Results.Ok(result.Value);

            var isPending = result.Error?.Contains("approval", StringComparison.OrdinalIgnoreCase) == true;
            var errorCode = isPending ? VoltflowTaxonomy.ErrorCodes.VF_01101 : VoltflowTaxonomy.ErrorCodes.VF_01102;
            return Results.Problem(
                statusCode: isPending ? StatusCodes.Status403Forbidden : StatusCodes.Status401Unauthorized,
                title: isPending ? "Account approval pending" : "Authentication failed",
                detail: result.Error ?? "Invalid credentials.",
                type: $"urn:voltflow:error:{errorCode}",
                extensions: new Dictionary<string, object?> { ["errorCode"] = errorCode });
        })
        .WithName("VF-01101_Login")
        .UseMutationRateLimit();

        auth.MapPost("/session/revoke", async (RevokeSessionRequest request, IAuthService service, CancellationToken ct) =>
        {
            var result = await service.RevokeSessionAsync(request.Token, ct);
            return result.From();
        })
        .WithName("VF-01301_RevokeSession")
        .UseMutationPolicy();

        auth.MapPost("/password-reset/request", async (PasswordResetRequest request, IAuthService service, CancellationToken ct) =>
        {
            var result = await service.RequestPasswordResetAsync(request.Email, ct);
            return result.From();
        })
        .WithName("VF-01401_PasswordResetRequest")
        .UseMutationRateLimit();

        auth.MapPost("/password-reset/complete", async (CompletePasswordResetRequest request, IAuthService service, CancellationToken ct) =>
        {
            var result = await service.CompletePasswordResetAsync(request.Token, request.NewPassword, request.Email, ct);
            return result.From();
        })
        .WithName("VF-01402_PasswordResetComplete")
        .UseMutationPolicy(RateLimitScope.Strict);

        auth.MapGet("/users", async (bool? approved, int? limit, int? offset, HttpContext httpContext, IAuthService service, CancellationToken ct) =>
        {
            var result = await service.ListUsersAsync(approved, limit, offset, ct);
            if (result.IsSuccess) httpContext.ApplyPaginationHeaders(result.Value!);
            return result.From(page => Results.Ok(page.Items));
        })
        .WithName("VF-01101_ListUsers")
        .RequireAuthorization("AdminOnly");

        auth.MapPost("/users/{id:guid}/approve", async (Guid id, IAuthService service, CancellationToken ct) =>
        {
            var result = await service.ApproveAsync(id, ct);
            return result.From();
        })
        .WithName("VF-01101_ApproveUser")
        .RequireAuthorization("AdminOnly")
        .UseMutationPolicy();

        auth.MapPost("/users/{id:guid}/roles", async (Guid id, AssignRoleRequest request, IAuthService service, CancellationToken ct) =>
        {
            var result = await service.AssignRoleAsync(id, request.RoleName, ct);
            return result.From();
        })
        .WithName("VF-01101_AssignRole")
        .RequireAuthorization("AdminOnly")
        .UseMutationPolicy();

        return routes;
    }
}
