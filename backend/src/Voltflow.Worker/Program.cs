using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;
using Voltflow.Worker;
using Microsoft.EntityFrameworkCore;
using Voltflow.Application.Interfaces;
using Voltflow.Infrastructure.Observability;
using Voltflow.Infrastructure.Persistence;
using Voltflow.Infrastructure.Repositories;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddVoltflowTelemetry(
    builder.Configuration,
    "voltflow-worker",
    tracing => tracing.AddSource(TelemetryServiceCollectionExtensions.WorkerActivitySourceName));
var connectionString = builder.Configuration["ConnectionStrings:DefaultConnection"]
	?? "Host=localhost;Port=5433;Database=voltflow;Username=postgres;Password=postgres";
builder.Services.AddDbContext<VoltflowDbContext>(options => options.UseNpgsql(connectionString));
// V7: the Worker has its own composition root, so the injected clock has to be registered here too -
// MaintenanceWorker resolves TimeProvider and, without this, failed on every cycle.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IReminderRepository, ReminderRepository>();
builder.Services.AddScoped<ReminderProcessor>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddScoped<IOutboxPublisher, AuditOutboxPublisher>();
builder.Services.AddScoped<OutboxProcessor>();
builder.Services.AddHostedService<ReminderWorker>();
builder.Services.AddHostedService<OutboxWorker>();


await builder.Build().RunAsync();