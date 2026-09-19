using System.Globalization;

namespace Voltflow.Api.Observability;

/// <summary>Backs the legacy hand-rolled `/metrics` text endpoint only. Request/failure/duration
/// metrics for OpenTelemetry come from the standard ASP.NET Core instrumentation (T1); the two
/// System.Diagnostics.Metrics counters that used to live here (underscore names, raw-path label,
/// never exported) duplicated it non-conformantly and were removed.</summary>
public sealed class ApiMetrics
{
    private long _requestCount;
    private long _failureCount;
    private long _durationMilliseconds;

    public void Record(string endpoint, int statusCode, long durationMilliseconds)
    {
        Interlocked.Increment(ref _requestCount);
        Interlocked.Add(ref _durationMilliseconds, durationMilliseconds);
        if (statusCode >= 400)
            Interlocked.Increment(ref _failureCount);
    }

    public string SnapshotPrometheus()
    {
        var requests = Volatile.Read(ref _requestCount);
        var failures = Volatile.Read(ref _failureCount);
        var duration = Volatile.Read(ref _durationMilliseconds);
        return $"# TYPE voltflow_http_requests_total counter\nvoltflow_http_requests_total {requests.ToString(CultureInfo.InvariantCulture)}\n# TYPE voltflow_http_failures_total counter\nvoltflow_http_failures_total {failures.ToString(CultureInfo.InvariantCulture)}\n# TYPE voltflow_http_duration_milliseconds counter\nvoltflow_http_duration_milliseconds {duration.ToString(CultureInfo.InvariantCulture)}\n";
    }
}
