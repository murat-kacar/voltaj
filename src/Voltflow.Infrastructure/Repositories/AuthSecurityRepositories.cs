using Microsoft.EntityFrameworkCore;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Identity;
using Voltflow.Infrastructure.Persistence;

namespace Voltflow.Infrastructure.Repositories;

public sealed class UserSessionRepository : IUserSessionRepository
{
    private readonly VoltflowDbContext _dbContext;
    public UserSessionRepository(VoltflowDbContext dbContext) => _dbContext = dbContext;

    public async Task AddAsync(UserSession session, CancellationToken ct = default)
    {
        _dbContext.UserSessions.Add(session);
        await _dbContext.SaveChangesAsync(ct);
    }

    public Task<UserSession?> GetByTokenAsync(string token, CancellationToken ct = default)
        => _dbContext.UserSessions.FirstOrDefaultAsync(x => x.TokenHash == UserSession.Hash(token), ct);

    public async Task UpdateAsync(UserSession session, CancellationToken ct = default)
    {
        _dbContext.UserSessions.Update(session);
        await _dbContext.SaveChangesAsync(ct);
    }
}

public sealed class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly VoltflowDbContext _dbContext;
    public PasswordResetTokenRepository(VoltflowDbContext dbContext) => _dbContext = dbContext;

    public async Task AddAsync(PasswordResetToken token, CancellationToken ct = default)
    {
        _dbContext.PasswordResetTokens.Add(token);
        await _dbContext.SaveChangesAsync(ct);
    }

    public Task<PasswordResetToken?> GetAsync(string token, CancellationToken ct = default)
        => _dbContext.PasswordResetTokens.FirstOrDefaultAsync(x => x.TokenHash == UserSession.Hash(token), ct);

    public async Task UpdateAsync(PasswordResetToken token, CancellationToken ct = default)
    {
        _dbContext.PasswordResetTokens.Update(token);
        await _dbContext.SaveChangesAsync(ct);
    }
}