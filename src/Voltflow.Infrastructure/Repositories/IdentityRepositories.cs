using Microsoft.EntityFrameworkCore;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Identity;
using Voltflow.Infrastructure.Persistence;

namespace Voltflow.Infrastructure.Repositories;

public sealed class AppUserRepository : Repository<AppUser>, IAppUserRepository
{
    public AppUserRepository(VoltflowDbContext dbContext) : base(dbContext) { }

    public Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct = default)
        => DbContext.AppUsers.FirstOrDefaultAsync(x => x.Email == email, ct);

    public override async Task<IReadOnlyList<AppUser>> ListAsync(CancellationToken ct = default)
        => await DbContext.AppUsers.OrderBy(x => x.Name).ToListAsync(ct);
}

public sealed class RoleRepository : Repository<AppRole>, IRoleRepository
{
    public RoleRepository(VoltflowDbContext dbContext) : base(dbContext) { }

    public Task<AppRole?> GetByNameAsync(string name, CancellationToken ct = default)
        => DbContext.AppRoles.FirstOrDefaultAsync(x => x.Name == name, ct);

    public async Task<IReadOnlyList<string>> GetNamesByUserAsync(Guid userId, CancellationToken ct = default)
        => await (from ur in DbContext.AppUserRoles
                  join r in DbContext.AppRoles on ur.RoleId equals r.Id
                  where ur.UserId == userId
                  select r.Name).ToListAsync(ct);

    public override async Task<IReadOnlyList<AppRole>> ListAsync(CancellationToken ct = default)
        => await DbContext.AppRoles.OrderBy(x => x.Name).ToListAsync(ct);
}

public sealed class UserRoleRepository : IUserRoleRepository
{
    private readonly VoltflowDbContext _db;

    public UserRoleRepository(VoltflowDbContext db) => _db = db;

    public Task<bool> ExistsAsync(Guid userId, Guid roleId, CancellationToken ct = default)
        => _db.AppUserRoles.AnyAsync(x => x.UserId == userId && x.RoleId == roleId, ct);

    public async Task AddAsync(AppUserRole userRole, CancellationToken ct = default)
    {
        await _db.AppUserRoles.AddAsync(userRole, ct);
        await _db.SaveChangesAsync(ct);
    }
}

