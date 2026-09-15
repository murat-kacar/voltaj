using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Voltflow.Worker;
using Microsoft.EntityFrameworkCore;
using Voltflow.Application.Interfaces;
using Voltflow.Infrastructure.Persistence;
using Voltflow.Infrastructure.Repositories;

var builder = Host.CreateApplicationBuilder(args);
var connectionString = builder.Configuration["ConnectionStrings:DefaultConnection"]
	?? "Host=localhost;Port=5433;Database=voltflow;Username=postgres;Password=postgres";
builder.Services.AddDbContext<VoltflowDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<IReminderRepository, ReminderRepository>();
builder.Services.AddScoped<ReminderProcessor>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddScoped<IOutboxPublisher, AuditOutboxPublisher>();
builder.Services.AddScoped<OutboxProcessor>();
builder.Services.AddScoped<IWorkOrderRepository, WorkOrderRepository>();
builder.Services.AddScoped<IMaintenanceContractRepository, MaintenanceContractRepository>();
builder.Services.AddScoped<MaintenanceProcessor>();
builder.Services.AddHostedService<ReminderWorker>();
builder.Services.AddHostedService<OutboxWorker>();
builder.Services.AddHostedService<MaintenanceWorker>();

await builder.Build().RunAsync();