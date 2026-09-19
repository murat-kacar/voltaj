using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Voltflow.Application.Interfaces;
using Voltflow.Infrastructure.Persistence;
using Voltflow.Worker;

namespace Voltflow.Tests;

/// <summary>T2: the W3C trace context must not break at a service boundary - the inbound HTTP boundary
/// and the asynchronous API -> Worker boundary (the transactional outbox).</summary>
public sealed class OutboxTraceContextTests
{
    private static ActivityListener ListenTo(string sourceName, ConcurrentBag<Activity>? started = null)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == sourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => started?.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    [Fact]
    public void OutboxMessage_ShouldCaptureTheProducingW3CTraceContext()
    {
        using var source = new ActivitySource("Voltflow.Tests.Producer");
        using var listener = ListenTo("Voltflow.Tests.Producer");
        using var producer = source.StartActivity("produce", ActivityKind.Server);
        Assert.NotNull(producer);

        var message = new OutboxMessage("WorkOrderCompleted", "{}");

        Assert.Equal(producer.Id, message.TraceParent);
        Assert.Matches("^00-[0-9a-f]{32}-[0-9a-f]{16}-[0-9a-f]{2}$", message.TraceParent);
    }

    [Fact]
    public void OutboxMessage_WithoutAnActivity_ShouldCarryNoTraceContext()
    {
        Activity.Current = null;

        var message = new OutboxMessage("WorkOrderCompleted", "{}");

        Assert.Null(message.TraceParent);
        Assert.Null(message.TraceState);
    }

    [Fact]
    public async Task OutboxProcessor_ShouldContinueTheProducersTrace()
    {
        var traceId = ActivityTraceId.CreateRandom();
        var producerSpanId = ActivitySpanId.CreateRandom();
        var item = new OutboxWorkItem(Guid.NewGuid(), "WorkOrderCompleted", "{}", 0, $"00-{traceId}-{producerSpanId}-01", "vendor=1");
        var started = new ConcurrentBag<Activity>();
        using var listener = ListenTo("Voltflow.Worker", started);
        var processor = new OutboxProcessor(new OneMessageRepository(item), new Publisher());

        await processor.ProcessDueAsync(DateTime.UtcNow);

        var consumer = Assert.Single(started, activity => activity.TraceId == traceId);
        Assert.Equal(ActivityKind.Consumer, consumer.Kind);
        Assert.Equal(producerSpanId, consumer.ParentSpanId);
        Assert.Equal("WorkOrderCompleted", consumer.GetTagItem("messaging.destination.name"));
        Assert.Equal("process", consumer.GetTagItem("messaging.operation.type"));
    }

    [Fact]
    public async Task OutboxProcessor_ShouldMarkTheConsumerSpanFailed_WhenPublishingThrows()
    {
        var traceId = ActivityTraceId.CreateRandom();
        var item = new OutboxWorkItem(Guid.NewGuid(), "Unknown", "{}", 0, $"00-{traceId}-{ActivitySpanId.CreateRandom()}-01");
        var started = new ConcurrentBag<Activity>();
        using var listener = ListenTo("Voltflow.Worker", started);
        var processor = new OutboxProcessor(new OneMessageRepository(item), new Publisher(fail: true));

        await processor.ProcessDueAsync(DateTime.UtcNow);

        var consumer = Assert.Single(started, activity => activity.TraceId == traceId);
        Assert.Equal(ActivityStatusCode.Error, consumer.Status);
        Assert.Equal(typeof(InvalidOperationException).FullName, consumer.GetTagItem("error.type"));
    }

    [Fact]
    public async Task OutboxProcessor_ShouldStartAFreshTrace_WhenTheMessageCarriesNoContext()
    {
        var item = new OutboxWorkItem(Guid.NewGuid(), "WorkOrderCompleted", "{}", 0);
        var started = new ConcurrentBag<Activity>();
        using var listener = ListenTo("Voltflow.Worker", started);
        var processor = new OutboxProcessor(new OneMessageRepository(item), new Publisher());

        await processor.ProcessDueAsync(DateTime.UtcNow);

        var consumer = Assert.Single(started, activity => Equals(activity.GetTagItem("messaging.message.id"), item.Id.ToString()));
        Assert.Equal(default, consumer.ParentSpanId);
    }

    private sealed class Publisher(bool fail = false) : IOutboxPublisher
    {
        public Task PublishAsync(OutboxWorkItem message, CancellationToken ct = default)
            => fail ? throw new InvalidOperationException("publish failed") : Task.CompletedTask;
    }

    private sealed class OneMessageRepository(OutboxWorkItem message) : IOutboxRepository
    {
        public Task<IReadOnlyList<OutboxWorkItem>> ListDueAsync(DateTime utcNow, int batchSize, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<OutboxWorkItem>>([message]);
        public Task AddAsync(OutboxWorkItem message, CancellationToken ct = default) => Task.CompletedTask;
        public Task QueueAsync(OutboxWorkItem message, CancellationToken ct = default) => Task.CompletedTask;
        public Task MarkProcessedAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
        public Task MarkFailedAsync(Guid id, string error, CancellationToken ct = default) => Task.CompletedTask;
        public Task RequeueAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<OutboxWorkItem>> ListDeadLetterAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<OutboxWorkItem>>([]);
    }
}

[Xunit.Collection("ApiIntegration")]
public sealed class InboundTraceContextTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public InboundTraceContextTests(ApiTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Request_WithATraceparent_ShouldBeServedInsideTheCallersTrace()
    {
        var traceId = ActivityTraceId.CreateRandom();
        var callerSpanId = ActivitySpanId.CreateRandom();
        var started = new ConcurrentBag<Activity>();
        var stopped = new ConcurrentBag<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => { if (activity.TraceId == traceId) started.Add(activity); },
            ActivityStopped = activity => { if (activity.TraceId == traceId) stopped.Add(activity); }
        };
        ActivitySource.AddActivityListener(listener);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/customers");
        request.Headers.Add("traceparent", $"00-{traceId}-{callerSpanId}-01");
        using var response = await _fixture.CreateApiClient().SendAsync(request);

        // T1: exactly one server span per request - the middleware enriches it, it does not add a second.
        Assert.Single(started, a => a.Kind == ActivityKind.Server);
        Assert.True(SpinWait.SpinUntil(() => stopped.Any(a => a.Kind == ActivityKind.Server), TimeSpan.FromSeconds(3)));
        var server = stopped.First(a => a.Kind == ActivityKind.Server);
        Assert.Equal(traceId, server.TraceId);
        Assert.Equal(callerSpanId, server.ParentSpanId);
        Assert.Equal(server.GetTagItem("voltflow.operation_id"), response.Headers.GetValues("X-Operation-Id").Single());
    }

    [Fact]
    public void OpenTelemetry_ShouldBeRegisteredInTheApiHost()
    {
        Assert.NotNull(_fixture.Services.GetService<TracerProvider>());
        Assert.NotNull(_fixture.Services.GetService<MeterProvider>());
    }
}
