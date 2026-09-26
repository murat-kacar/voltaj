using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Voltflow.Infrastructure.Observability;

/// <summary>M9/T1: the single OpenTelemetry registration shared by the API and the Worker.</summary>
public static class TelemetryServiceCollectionExtensions
{
    public const string WorkerActivitySourceName = "Voltflow.Worker";

    public static IServiceCollection AddVoltflowTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        Action<TracerProviderBuilder>? configureTracing = null,
        Action<MeterProviderBuilder>? configureMetrics = null)
    {
        var telemetry = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing => configureTracing?.Invoke(tracing))
            .WithMetrics(metrics => configureMetrics?.Invoke(metrics));

        // O4: the collector endpoint comes from the environment; unset means spans are still recorded
        // in-process but nothing tries to connect (tests, local runs without a collector).
        if (!string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
            telemetry.UseOtlpExporter();

        return services;
    }
}
