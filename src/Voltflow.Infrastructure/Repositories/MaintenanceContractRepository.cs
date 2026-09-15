using Microsoft.EntityFrameworkCore;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.WorkOrders;
using Voltflow.Infrastructure.Persistence;

namespace Voltflow.Infrastructure.Repositories;

public sealed class MaintenanceContractRepository : IMaintenanceContractRepository
{
    private readonly VoltflowDbContext _context;

    public MaintenanceContractRepository(VoltflowDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<MaintenanceContract>> GetDueContractsAsync(DateTime currentDate, CancellationToken ct = default)
    {
        return await _context.Set<MaintenanceContract>()
            .Where(c => c.IsActive && c.NextMaintenanceDate <= currentDate)
            .ToListAsync(ct);
    }

    public async Task UpdateAsync(MaintenanceContract contract, CancellationToken ct = default)
    {
        _context.Set<MaintenanceContract>().Update(contract);
        await _context.SaveChangesAsync(ct);
    }
}
