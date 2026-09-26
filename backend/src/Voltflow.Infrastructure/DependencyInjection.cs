using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Voltflow.Application.Interfaces;
using Voltflow.Infrastructure.Persistence;
using Voltflow.Infrastructure.RateLimiting;
using Voltflow.Infrastructure.Repositories;
using Voltflow.Infrastructure.Security;

namespace Voltflow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5433;Database=voltflow;Username=postgres;Password=postgres";

        services.AddDbContext<VoltflowDbContext>((sp, options) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            if (config["Database:Provider"] == "InMemory")
            {
                var dbName = config["Database:Name"] ?? "VoltflowDb_Default";
                options.UseInMemoryDatabase(dbName)
                    .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
            }
            else
            {
                options.UseNpgsql(connectionString);
            }
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<VoltflowDbContext>());
        // V7: system clock is injected, not called directly - tests override this with FakeTimeProvider.
        services.TryAddSingleton(TimeProvider.System);

        // Repositories
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ICustomerSiteRepository, CustomerSiteRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IDocumentNumbers, DocumentNumbers>();
        services.AddScoped<IQuickSaleRepository, QuickSaleRepository>();
        services.AddScoped<IAppUserRepository, AppUserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IExecutionGuard, ExecutionGuardRepository>();
        services.AddScoped<ICommandJournal, CommandJournal>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IReminderRepository, ReminderRepository>();

        // Services & Cache
        if (configuration["Database:Provider"] == "InMemory")
        {
            services.AddScoped<ISessionCacheService, DirectDbSessionCacheService>();
            services.AddSingleton<IDistributedRateLimiter, InMemoryRateLimiter>();
        }
        else
        {
            services.AddScoped<ISessionCacheService, RedisSessionCacheService>();
            services.AddSingleton<IDistributedRateLimiter, RedisRateLimiter>();
        }

        return services;
    }
}
