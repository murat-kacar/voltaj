using Microsoft.EntityFrameworkCore;
using Voltflow.Application.Common;
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

    public async Task<PagedResult<AppUser>> ListPagedAsync(bool? approved, int limit, int offset, CancellationToken ct = default)
    {
        var query = DbContext.AppUsers.AsQueryable();
        if (approved is not null) query = query.Where(x => x.IsApproved == approved);
        var ordered = query.OrderBy(x => x.Name).ThenBy(x => x.Id);
        var total = await ordered.CountAsync(ct);
        var items = await ordered.Skip(offset).Take(limit).ToListAsync(ct);
        return new PagedResult<AppUser>(items, total, limit, offset);
    }
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

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> GetNamesByUsersAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct = default)
    {
        if (userIds.Count == 0) return new Dictionary<Guid, IReadOnlyList<string>>();
        var rows = await (from ur in DbContext.AppUserRoles
                          join r in DbContext.AppRoles on ur.RoleId equals r.Id
                          where userIds.Contains(ur.UserId)
                          select new { ur.UserId, r.Name }).ToListAsync(ct);
        return rows
            .GroupBy(row => row.UserId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<string>)group.Select(row => row.Name).Order().ToList());
    }

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

