using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Voltflow.Infrastructure.Persistence;

public sealed class VoltflowDbContextFactory : IDesignTimeDbContextFactory<VoltflowDbContext>
{
    public VoltflowDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("VOLTFlow_CONNECTION_STRING")
            ?? "Host=localhost;Port=5433;Database=voltflow;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<VoltflowDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new VoltflowDbContext(optionsBuilder.Options);
    }
}
