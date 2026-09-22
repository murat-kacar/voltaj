using Microsoft.EntityFrameworkCore;
using Voltflow.Application.Interfaces;

namespace Voltflow.Infrastructure.Persistence;

public sealed partial class VoltflowDbContext : DbContext, IUnitOfWork
{
    private readonly ICurrentUser? _currentUser;
    private readonly IOperationContext? _operationContext;
    private readonly TimeProvider _timeProvider;

    public Task<int> CommitAsync(CancellationToken ct = default) => SaveChangesAsync(ct);


    public VoltflowDbContext(
        DbContextOptions<VoltflowDbContext> options,
        ICurrentUser? currentUser = null,
        IOperationContext? operationContext = null,
        TimeProvider? timeProvider = null) : base(options)
    {
        _currentUser = currentUser;
        _operationContext = operationContext;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VoltflowDbContext).Assembly);

        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(type => typeof(Voltflow.Domain.Common.Entity).IsAssignableFrom(type.ClrType)))
        {
            modelBuilder.Entity(entityType.ClrType)
                .Property(nameof(Voltflow.Domain.Common.Entity.Version))
                .IsConcurrencyToken()
                .HasDefaultValue(1L);
        }
    }
}
