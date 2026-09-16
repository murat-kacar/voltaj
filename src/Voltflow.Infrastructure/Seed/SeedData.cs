using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Voltflow.Domain.Identity;
using Voltflow.Domain.Common;
using Voltflow.Domain.Customers;
using Voltflow.Domain.Finance;
using Voltflow.Domain.Inventory;
using Voltflow.Infrastructure.Persistence;

namespace Voltflow.Infrastructure.Seed;

public static class SeedData
{
    // Canonical VUT Seed Identifiers (13. Halka)
    public const string SEED_01101_AdminUser = "SEED_01101_AdminUser";
    public const string SEED_02101_ActiveCustomer = "SEED_02101_ActiveCustomer";
    public const string SEED_04101_MainWarehouseStock = "SEED_04101_MainWarehouseStock";
    public const string SEED_05101_SalesInvoice = "SEED_05101_SalesInvoice";

    private static readonly string[] RoleNames = ["Admin", "Manager", "Technician", "Viewer"];
    private static readonly (string Type, string Code, string Name)[] ReferenceValues =
    [
        ("payment_method", "CASH", "Cash"),
        ("payment_method", "BANK_TRANSFER", "Bank transfer"),
        ("payment_method", "CARD", "Card"),
        ("work_order_priority", "NORMAL", "Normal"),
        ("work_order_priority", "HIGH", "High"),
        ("stock_movement_type", "IN", "Stock in"),
        ("stock_movement_type", "OUT", "Stock out"),
        ("stock_movement_type", "NEUTRAL", "Neutral"),
        ("reminder_type", "PAYMENT_DUE", "Payment due"),
        ("reminder_type", "QUOTE_EXPIRY", "Quote expiry"),
        ("reminder_type", "WORK_ORDER_VISIT", "Work order visit")
    ];

    public static async Task ApplyAsync(
        VoltflowDbContext dbContext,
        IConfiguration configuration,
        CancellationToken ct = default)
    {
        if (dbContext.Database.IsRelational())
            await dbContext.Database.MigrateAsync(ct);
        else
            await dbContext.Database.EnsureCreatedAsync(ct);

        foreach (var roleName in RoleNames)
        {
            if (!await dbContext.AppRoles.AnyAsync(x => x.Name == roleName, ct))
                dbContext.AppRoles.Add(new AppRole(roleName));
        }

            foreach (var reference in ReferenceValues)
            {
                if (!await dbContext.ReferenceValues.AnyAsync(x => x.Type == reference.Type && x.Code == reference.Code, ct))
                dbContext.ReferenceValues.Add(new ReferenceValue(reference.Type, reference.Code, reference.Name));
            }

        await dbContext.SaveChangesAsync(ct);

        var email = configuration["Seed:AdminEmail"]?.Trim().ToLowerInvariant() ?? "admin@voltflow.com";
        var password = configuration["Seed:AdminPassword"] ?? "Admin123!";
        var name = configuration["Seed:AdminName"]?.Trim() ?? "System Admin";


        var user = await dbContext.AppUsers.SingleOrDefaultAsync(x => x.Email == email, ct);
        if (user is null)
        {
            user = new AppUser(name, email);
            user.SetPasswordHash(new PasswordHasher<AppUser>().HashPassword(user, password));
            user.SetVerified();
            user.Approve();
            dbContext.AppUsers.Add(user);
            await dbContext.SaveChangesAsync(ct);
        }

        var adminRole = await dbContext.AppRoles.SingleAsync(x => x.Name == "Admin", ct);
        if (!await dbContext.AppUserRoles.AnyAsync(x => x.UserId == user.Id && x.RoleId == adminRole.Id, ct))
        {
            dbContext.AppUserRoles.Add(new AppUserRole(user.Id, adminRole.Id));
            await dbContext.SaveChangesAsync(ct);
        }


    }
}
