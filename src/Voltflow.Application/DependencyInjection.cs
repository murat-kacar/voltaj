using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Voltflow.Application.Interfaces;
using Voltflow.Application.Services;
using Voltflow.Domain.Identity;

namespace Voltflow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ICustomerSiteService, CustomerSiteService>();
        services.AddScoped<IQuoteService, QuoteService>();
        services.AddScoped<IWorkOrderService, WorkOrderService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IBillingService, BillingService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IQuickSaleService, QuickSaleService>();
        services.AddScoped<ICashShiftService, CashShiftService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IReminderService, ReminderService>();
        services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();

        return services;
    }
}
