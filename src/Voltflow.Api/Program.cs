using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Voltflow.Api.Endpoints;
using Voltflow.Api.Diagnostics;
using Voltflow.Api.Errors;
using Voltflow.Api.Health;
using Voltflow.Api.Security;
using Voltflow.Api.Observability;
using Voltflow.Application;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Identity;
using Voltflow.Infrastructure;
using Voltflow.Infrastructure.Persistence;
using Voltflow.Infrastructure.Observability;
using Voltflow.Infrastructure.Seed;
using Voltflow.Infrastructure.RateLimiting;
using Voltflow.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// G3: PII redaction — registers IRedactor in DI. ErasingRedactor replaces any value
// tagged with PersonalDataTaxonomy.PrivateData with "***".
// Usage: inject IRedactor and call redactor.Redact(value, PersonalDataTaxonomy.PrivateData)
// or annotate source-generated [LoggerMessage] parameters with [PrivateData].
builder.Services.AddRedaction(x =>
    x.SetRedactor<ErasingRedactor>(new DataClassificationSet(PersonalDataTaxonomy.PrivateData)));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        var operationContext = context.HttpContext.RequestServices.GetService<IOperationContext>();
        context.ProblemDetails.Extensions["traceId"] = System.Diagnostics.Activity.Current?.TraceId.ToString()
            ?? context.HttpContext.TraceIdentifier;
        context.ProblemDetails.Extensions["operationId"] = operationContext?.OperationId.ToString();
    };
});
builder.Services.AddRequestTimeouts(options =>
{
    options.DefaultPolicy = new Microsoft.AspNetCore.Http.Timeouts.RequestTimeoutPolicy
    {
        Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue<int>("ExecutionPolicies:DefaultTimeoutSeconds", 30)),
        TimeoutStatusCode = 503
    };
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ApiMetrics>();
builder.Services.AddSingleton<StartupState>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IOperationContext, OperationContext>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    if (builder.Environment.IsDevelopment())
        jwtKey = "development-only-voltflow-key-32-chars";
    else
        throw new InvalidOperationException("Jwt:Key must be configured outside Development.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var authorization = context.HttpContext.Request.Headers.Authorization.ToString();
                    var rawToken = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                        ? authorization[7..].Trim()
                        : null;
                    if (string.IsNullOrWhiteSpace(rawToken))
                    {
                        context.Fail("Session token is unavailable.");
                        return;
                    }

                    var sessionCache = context.HttpContext.RequestServices.GetRequiredService<ISessionCacheService>();
                    var isValid = await sessionCache.IsSessionValidAsync(rawToken, context.HttpContext.RequestAborted);
                    if (!isValid)
                        context.Fail("Session is revoked or expired.");
                }
            };
            options.TokenValidationParameters = new()
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateIssuer = !string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Issuer"]),
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidateAudience = !string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Audience"]),
                ValidAudience = builder.Configuration["Jwt:Audience"],
                ValidateLifetime = true
            };
        });
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser())
    .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"))
    .AddPolicy("WriteAccess", policy => policy.RequireRole("Admin", "Manager"))
    .AddPolicy("OperationsAccess", policy => policy.RequireRole("Admin", "Manager", "Technician"));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// T1/T2: the probes are noise in traces; everything else gets the standard ASP.NET Core server span
// (which also extracts an incoming W3C traceparent).
string[] probePaths = ["/health", "/ready", "/startup", "/metrics"];
builder.Services.AddVoltflowTelemetry(
    builder.Configuration,
    "voltflow-api",
    tracing => tracing.AddAspNetCoreInstrumentation(options =>
        options.Filter = httpContext => !probePaths.Any(path => httpContext.Request.Path.StartsWithSegments(path))),
    metrics => metrics.AddAspNetCoreInstrumentation());

var app = builder.Build();

app.UseRequestTimeouts();
app.UseExceptionHandler();

if (app.Configuration.GetValue("Database:SeedOnStartup", false))
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
    await SeedData.ApplyAsync(dbContext, app.Configuration, app.Environment, app.Logger);
}

app.Services.GetRequiredService<StartupState>().MarkCompleted();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseStatusCodePages(async statusContext =>
{
    var httpContext = statusContext.HttpContext;
    var response = httpContext.Response;
    if (response.StatusCode is not (StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden)
        || response.HasStarted)
        return;

    var operationContext = httpContext.RequestServices.GetRequiredService<IOperationContext>();
    var isForbidden = response.StatusCode == StatusCodes.Status403Forbidden;
    response.ContentType = "application/problem+json";
    var problemDetails = new ProblemDetails
    {
        Status = response.StatusCode,
        Title = isForbidden ? "Forbidden" : "Unauthorized",
        Detail = isForbidden ? "You do not have permission to perform this operation." : "Authentication is required.",
        Type = $"https://voltflow.dev/problems/{(isForbidden ? "forbidden" : "unauthorized")}",
        Instance = httpContext.Request.Path,
        Extensions =
        {
            ["traceId"] = System.Diagnostics.Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier,
            ["operationId"] = operationContext.OperationId.ToString()
        }
    };
    await response.WriteAsync(JsonSerializer.Serialize(problemDetails), Encoding.UTF8);
});
if (!string.IsNullOrWhiteSpace(jwtKey))
    app.UseAuthentication();
app.UseMiddleware<OperationTraceMiddleware>();
app.UseAuthorization();
app.MapAuthEndpoints();
app.MapCustomerEndpoints();
app.MapServiceEndpoints();
app.MapInventoryEndpoints();
app.MapPaymentEndpoints();
app.MapReminderEndpoints();
app.MapProductEndpoints();
app.MapOperationsEndpoints();
app.MapAuditLogEndpoints();
app.MapTestDataEndpoints();
app.MapSalesEndpoints();

app.MapHealthEndpoints();
app.MapGet("/metrics", (ApiMetrics metrics) => Results.Text(metrics.SnapshotPrometheus(), "text/plain; version=0.0.4"));

app.Run();

public partial class Program;
