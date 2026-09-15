using System.Diagnostics.Metrics;
using System.Globalization;

namespace Voltflow.Api.Observability;

public sealed class ApiMetrics
{
    private readonly Counter<long> _requests;
    private readonly Counter<long> _failures;
    private long _requestCount;
    private long _failureCount;
    private long _durationMilliseconds;

    public ApiMetrics()
    {
        var meter = new Meter("Voltflow.Api", "1.0.0");
        _requests = meter.CreateCounter<long>("voltflow_http_requests_total");
        _failures = meter.CreateCounter<long>("voltflow_http_failures_total");
    }

    public void Record(string endpoint, int statusCode, long durationMilliseconds)
    {
        Interlocked.Increment(ref _requestCount);
        Interlocked.Add(ref _durationMilliseconds, durationMilliseconds);
        _requests.Add(1, new KeyValuePair<string, object?>("endpoint", endpoint), new KeyValuePair<string, object?>("status_code", statusCode));
        if (statusCode >= 400)
        {
            Interlocked.Increment(ref _failureCount);
            _failures.Add(1, new KeyValuePair<string, object?>("endpoint", endpoint), new KeyValuePair<string, object?>("status_code", statusCode));
        }
    }

    public string SnapshotPrometheus()
    {
        var requests = Volatile.Read(ref _requestCount);
        var failures = Volatile.Read(ref _failureCount);
        var duration = Volatile.Read(ref _durationMilliseconds);
        return $"# TYPE voltflow_http_requests_total counter\nvoltflow_http_requests_total {requests.ToString(CultureInfo.InvariantCulture)}\n# TYPE voltflow_http_failures_total counter\nvoltflow_http_failures_total {failures.ToString(CultureInfo.InvariantCulture)}\n# TYPE voltflow_http_duration_milliseconds counter\nvoltflow_http_duration_milliseconds {duration.ToString(CultureInfo.InvariantCulture)}\n";
    }
}
