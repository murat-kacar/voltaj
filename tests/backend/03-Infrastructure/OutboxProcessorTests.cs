using Voltflow.Application.Interfaces;
using Voltflow.Worker;

namespace Voltflow.Tests;

[Trait("VUT", "07201")]
public sealed class OutboxProcessorTests
{
    [Fact]
    [Trait("VUT", "07201")]
    public async Task ProcessDueAsync_publishes_and_marks_successful_messages_processed()
    {
        var first = new OutboxWorkItem(Guid.NewGuid(), "AuditEventRecorded", "{}", 0);
        var second = new OutboxWorkItem(Guid.NewGuid(), "AuditEventRecorded", "{\"id\":2}", 1);
        var repository = new RecordingRepository(first, second);
        var publisher = new RecordingPublisher();
        var processor = new OutboxProcessor(repository, publisher);

        var count = await processor.ProcessDueAsync(DateTime.UtcNow);

        Assert.Equal(2, count);
        Assert.Equal(new[] { first.Id, second.Id }, publisher.Published.Select(x => x.Id));
        Assert.Equal(new[] { first.Id, second.Id }, repository.Processed);
        Assert.Empty(repository.Failed);
        Assert.Equal(100, repository.BatchSize);
    }

    [Fact]
    public async Task ProcessDueAsync_marks_failed_message_and_continues()
    {
        var failed = new OutboxWorkItem(Guid.NewGuid(), "Unknown", "{}", 2);
        var successful = new OutboxWorkItem(Guid.NewGuid(), "AuditEventRecorded", "{}", 0);
        var repository = new RecordingRepository(failed, successful);
        var publisher = new RecordingPublisher(failed.Id);
        var processor = new OutboxProcessor(repository, publisher);

        var count = await processor.ProcessDueAsync(DateTime.UtcNow);

        Assert.Equal(2, count);
        Assert.Equal(new[] { successful.Id }, repository.Processed);
        var failure = Assert.Single(repository.Failed);
        Assert.Equal(failed.Id, failure.Id);
        Assert.Equal("publish failed", failure.Error);
        Assert.Contains(successful.Id, publisher.Published.Select(x => x.Id));
    }

    private sealed class RecordingPublisher(Guid? failingId = null) : IOutboxPublisher
    {
        public List<OutboxWorkItem> Published { get; } = [];

        public Task PublishAsync(OutboxWorkItem message, CancellationToken ct = default)
        {
            if (message.Id == failingId)
                throw new InvalidOperationException("publish failed");

            Published.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingRepository(params OutboxWorkItem[] messages) : IOutboxRepository
    {
        public int BatchSize { get; private set; }
        public List<Guid> Processed { get; } = [];
        public List<(Guid Id, string Error)> Failed { get; } = [];

        public Task AddAsync(OutboxWorkItem message, CancellationToken ct = default) => Task.CompletedTask;
        public Task QueueAsync(OutboxWorkItem message, CancellationToken ct = default) => Task.CompletedTask;

        public Task<IReadOnlyList<OutboxWorkItem>> ListDueAsync(DateTime utcNow, int batchSize, CancellationToken ct = default)
        {
            BatchSize = batchSize;
            return Task.FromResult<IReadOnlyList<OutboxWorkItem>>(messages);
        }

        public Task MarkProcessedAsync(Guid id, CancellationToken ct = default)
        {
            Processed.Add(id);
            return Task.CompletedTask;
        }

        public Task MarkFailedAsync(Guid id, string error, CancellationToken ct = default)
        {
            Failed.Add((id, error));
            return Task.CompletedTask;
        }
        public Task RequeueAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<OutboxWorkItem>> ListDeadLetterAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<OutboxWorkItem>>(Array.Empty<OutboxWorkItem>());
    }
}

