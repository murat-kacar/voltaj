using Moq;
using Voltflow.Application.Dtos;
using Voltflow.Application.Services;
using Voltflow.Domain.Customers;
using Voltflow.Domain.Quotes;
using Voltflow.Domain.WorkOrders;
using Voltflow.Domain.Finance;
using Voltflow.Domain.Reminders;
using Voltflow.Domain.Identity;
using Voltflow.Application.Interfaces;
using Voltflow.Worker;
using Voltflow.Infrastructure.Persistence;

namespace Voltflow.Tests;

public class ArchitectureRefactorTests
{
    [Fact]
    public async Task CustomerService_CreateAsync_ShouldRejectEmptyFullName()
    {
        var repo = new FakeCustomerRepository();
        var service = new CustomerService(repo, new Mock<ICommandJournal>().Object, new Mock<IOperationContext>().Object);

        var result = await service.CreateAsync(new CreateCustomerRequest("", "user@example.com", "5551234"));

        Assert.False(result.IsSuccess);
        Assert.Equal("FullName is required.", result.Error);
    }

    [Fact]
    public async Task QuoteService_CreateAsync_ShouldFail_WhenTitleIsEmpty()
    {
        var repo = new FakeQuoteRepository();
        var service = new QuoteService(repo, new Mock<ICommandJournal>().Object, new Mock<IOperationContext>().Object);

        var result = await service.CreateAsync(new CreateQuoteRequest(Guid.NewGuid(), " "));

        Assert.False(result.IsSuccess);
        Assert.Equal("Title is required.", result.Error);
    }

    [Fact]
    public void Quote_ShouldRejectAcceptBeforeIssue()
    {
        var quote = new Quote(Guid.NewGuid(), "Test quote");

        Assert.Throws<InvalidOperationException>(() => quote.Accept());
    }

    [Fact]
    public void Quote_ShouldRequireItemBeforeIssue()
    {
        var quote = new Quote(Guid.NewGuid(), "Test quote");

        Assert.Throws<InvalidOperationException>(() => quote.Issue());
    }

    [Fact]
    public void Quote_ShouldRequireRejectionReason()
    {
        var quote = new Quote(Guid.NewGuid(), "Test quote");
        quote.AddItem("Service", 1, 100);
        quote.Issue();

        Assert.Throws<ArgumentException>(() => quote.Reject(" "));
    }

    [Fact]
    public void Quote_ShouldPersistRejectionReason()
    {
        var quote = new Quote(Guid.NewGuid(), "Rejected quote");
        quote.AddItem("Service", 1, 100);
        quote.Issue();

        quote.Reject("Customer declined the offer.");

        Assert.Equal("Customer declined the offer.", quote.RejectionReason);
        Assert.Equal(QuoteState.Rejected, quote.State);
    }

    [Fact]
    public void NewUser_ShouldRemainPendingUntilAdminApproval()
    {
        var user = new AppUser("Pending user", "pending@example.com");

        Assert.False(user.IsApproved);
        user.Approve();
        Assert.True(user.IsApproved);
    }

    [Fact]
    public void Session_ShouldBeRevocableAndResetToken_ShouldBeSingleUse()
    {
        var userId = Guid.NewGuid();
        var session = new UserSession(userId, "session-secret", DateTime.UtcNow.AddHours(1));
        var reset = new PasswordResetToken(userId, "reset-secret", DateTime.UtcNow.AddMinutes(30));

        Assert.True(session.IsActive);
        session.Revoke();
        Assert.False(session.IsActive);
        Assert.True(reset.IsUsable);
        reset.MarkUsed();
        Assert.False(reset.IsUsable);
    }

    [Fact]
    public void WorkOrder_ShouldFollowAssignmentAndCompletionTransitions()
    {
        var workOrder = new WorkOrder(Guid.NewGuid(), "Installation");

        Assert.Throws<InvalidOperationException>(() => workOrder.Complete("Sig", "photo.png"));
        workOrder.Assign(Guid.NewGuid());
        Assert.Throws<InvalidOperationException>(() => workOrder.Complete("Sig", "photo.png"));
        
        Assert.Throws<InvalidOperationException>(() => workOrder.Start());
        workOrder.CompleteSafetyChecklist();
        workOrder.Start();

        Assert.Throws<InvalidOperationException>(() => workOrder.Complete());
        workOrder.Complete(signature: "Customer Signature", photoUrl: "https://photos.voltflow.dev/pow1.jpg");

        Assert.Equal(WorkOrderStatus.Completed, workOrder.Status);
    }

    [Fact]
    public void InvoiceAndPayment_ShouldRejectOverAllocation()
    {
        var payment = new CustomerPayment(Guid.NewGuid(), 100, "Cash", DateOnly.FromDateTime(DateTime.UtcNow));
        var invoice = new SalesInvoice(payment.CustomerId, "INV-1", 80, DateOnly.FromDateTime(DateTime.UtcNow));

        payment.Allocate(80);
        invoice.Allocate(80);

        Assert.Throws<InvalidOperationException>(() => payment.Allocate(21));
        Assert.Throws<InvalidOperationException>(() => invoice.Allocate(1));
    }

    [Fact]
    public void ProgressBilling_ShouldCalculateNetPayableAfterApproval()
    {
        var billing = new ProgressBilling(Guid.NewGuid(), "PB-1", 1000, 100);

        billing.Approve(900);

        Assert.Equal(800, billing.NetPayableAmount);
    }

    [Fact]
    public void Reminder_ShouldMoveThroughPendingCompletedLifecycle()
    {
        var reminder = new ReminderRecord("PAYMENT_DUE", "Invoice", Guid.NewGuid(), DateTime.UtcNow, "Payment due");

        reminder.RegisterAttempt();
        reminder.Complete("Delivered");

        Assert.Equal(ReminderState.Completed, reminder.State);
        Assert.Equal(1, reminder.Attempts);
    }

    [Fact]
    public void AuditEvent_ShouldPreserveLineageAndSerializedSnapshots()
    {
        var operationId = Guid.NewGuid();
        var parentOperationId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var audit = new AuditEvent(
            operationId,
            parentOperationId,
            Guid.NewGuid(),
            "UPDATED",
            "Customer",
            entityId,
            "POST /api/customers/1",
            "CustomerDetail",
            "SaveCustomer",
            "{\"Name\":\"Before\"}",
            "{\"Name\":\"After\"}",
            "[\"Name\"]");

        Assert.Equal(operationId, audit.OperationId);
        Assert.Equal(parentOperationId, audit.ParentOperationId);
        Assert.Equal("CustomerDetail", audit.SourceScreen);
        Assert.Equal("SaveCustomer", audit.SourceAction);
        Assert.Contains("Before", audit.BeforeJson);
        Assert.Contains("After", audit.AfterJson);
        Assert.Contains("Name", audit.ChangedFieldsJson);
    }

    [Fact]
    public void OutboxMessage_ShouldRecordUnprocessedEventAndAttempts()
    {
        var message = new OutboxMessage("CustomerUpdated", "{\"entityId\":\"1\"}");

        message.MarkAttempt("provider unavailable");

        Assert.Equal(1, message.Attempts);
        Assert.Null(message.ProcessedAt);
        Assert.Equal("provider unavailable", message.LastError);

        message.MarkAttempt();

        Assert.NotNull(message.ProcessedAt);
        Assert.Equal(2, message.Attempts);
    }

    [Fact]
    public void OutboxMessage_ShouldDeadLetterAfterFiveFailures()
    {
        var message = new OutboxMessage("RetryEvent", "{}");

        for (var attempt = 0; attempt < OutboxMessage.MaxAttempts; attempt++)
            message.MarkAttempt("provider unavailable");

        Assert.Equal(OutboxState.DeadLetter, message.State);
        Assert.Equal(OutboxMessage.MaxAttempts, message.Attempts);
    }

    [Fact]
    public void Entity_ShouldIncrementConcurrencyVersion()
    {
        var customer = new Customer("Customer", "customer@example.com", "5551234");

        Assert.Equal(1, customer.Version);
        customer.Update("Updated customer", "customer@example.com", "5551234");
        customer.IncrementVersion();

        Assert.Equal(2, customer.Version);
    }

    [Fact]
    public async Task OutboxProcessor_ShouldMarkSupportedMessageProcessed()
    {
        var message = new OutboxWorkItem(Guid.NewGuid(), "AuditEventRecorded", "{}", 0);
        var repository = new FakeOutboxRepository(message);
        var processor = new OutboxProcessor(repository, new SuccessfulOutboxPublisher());

        var count = await processor.ProcessDueAsync(DateTime.UtcNow);

        Assert.Equal(1, count);
        Assert.Equal(message.Id, repository.ProcessedId);
    }

    [Fact]
    public async Task OutboxProcessor_ShouldMarkUnsupportedMessageFailed()
    {
        var message = new OutboxWorkItem(Guid.NewGuid(), "UnknownEvent", "{}", 0);
        var repository = new FakeOutboxRepository(message);
        var processor = new OutboxProcessor(repository, new FailingOutboxPublisher());

        var count = await processor.ProcessDueAsync(DateTime.UtcNow);

        Assert.Equal(1, count);
        Assert.Equal(message.Id, repository.FailedId);
    }

    [Fact]
    public void Guard_AgainstEmptyGuid_ShouldThrowWhenEmpty()
    {
        Assert.Throws<ArgumentException>(() => Voltflow.Domain.Common.Guard.AgainstEmptyGuid(Guid.Empty, "testId"));
    }

    [Fact]
    public void Guard_AgainstNegativeOrZero_ShouldThrowWhenZeroOrNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Voltflow.Domain.Common.Guard.AgainstNegativeOrZero(0, "amount"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Voltflow.Domain.Common.Guard.AgainstNegativeOrZero(-5, "amount"));
    }

    [Fact]
    public void WorkOrder_HoldAndCancel_ShouldWorkCorrectly()
    {
        var order = new WorkOrder(Guid.NewGuid(), "HVAC Service");
        order.Assign(Guid.NewGuid());
        order.CompleteSafetyChecklist();
        order.Start();

        order.PutOnHold("Waiting for parts");
        Assert.Equal(WorkOrderStatus.OnHold, order.Status);
        Assert.Equal("Waiting for parts", order.HoldReason);

        order.Cancel("Customer decided not to proceed");
        Assert.Equal(WorkOrderStatus.Cancelled, order.Status);
        Assert.Equal("Customer decided not to proceed", order.CancellationReason);
    }

    private sealed class FakeCustomerRepository : ICustomerRepository
    {
        public Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult<Customer?>(null);
        public Task<IReadOnlyList<Customer>> ListAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Customer>>(Array.Empty<Customer>());
        public Task AddAsync(Customer entity, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(Customer entity, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeleteAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
        public Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default) => Task.FromResult<Customer?>(null);
        public Task<Customer?> GetByTaxNumberAsync(string taxNumber, CancellationToken ct = default) => Task.FromResult<Customer?>(null);
        public Task<Voltflow.Application.Common.PagedResult<Customer>> ListPagedAsync(CustomerFilter filter, int limit, int offset, CancellationToken ct = default)
            => Task.FromResult(new Voltflow.Application.Common.PagedResult<Customer>(Array.Empty<Customer>(), 0, limit, offset));
    }

    private sealed class FakeQuoteRepository : IQuoteRepository
    {
        public Task<Quote?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult<Quote?>(null);
        public Task<IReadOnlyList<Quote>> ListAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Quote>>(Array.Empty<Quote>());
        public Task AddAsync(Quote entity, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(Quote entity, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeleteAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
        public Task<Quote?> GetByNumberAsync(string number, CancellationToken ct = default) => Task.FromResult<Quote?>(null);
        public Task<IReadOnlyList<Quote>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Quote>>(Array.Empty<Quote>());
        public Task<WorkOrder> ConvertAcceptedToWorkOrderAsync(Guid quoteId, CancellationToken ct = default)
            => throw new NotSupportedException();
        public Task<Voltflow.Application.Common.PagedResult<Quote>> ListPagedAsync(int limit, int offset, Guid? customerId, CancellationToken ct = default)
            => Task.FromResult(new Voltflow.Application.Common.PagedResult<Quote>(Array.Empty<Quote>(), 0, limit, offset));
    }

    private sealed class FakeOutboxRepository : IOutboxRepository
    {
        private readonly OutboxWorkItem _message;

        public FakeOutboxRepository(OutboxWorkItem message) => _message = message;

        public Guid? ProcessedId { get; private set; }
        public Guid? FailedId { get; private set; }

        public Task<IReadOnlyList<OutboxWorkItem>> ListDueAsync(DateTime utcNow, int batchSize, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<OutboxWorkItem>>([_message]);

        public Task AddAsync(OutboxWorkItem message, CancellationToken ct = default) => Task.CompletedTask;
        public Task QueueAsync(OutboxWorkItem message, CancellationToken ct = default) => Task.CompletedTask;

        public Task MarkProcessedAsync(Guid id, CancellationToken ct = default)
        {
            ProcessedId = id;
            return Task.CompletedTask;
        }

        public Task MarkFailedAsync(Guid id, string error, CancellationToken ct = default)
        {
            FailedId = id;
            return Task.CompletedTask;
        }

        public Task RequeueAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<OutboxWorkItem>> ListDeadLetterAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<OutboxWorkItem>>(Array.Empty<OutboxWorkItem>());
    }

    private sealed class SuccessfulOutboxPublisher : IOutboxPublisher
    {
        public Task PublishAsync(OutboxWorkItem message, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FailingOutboxPublisher : IOutboxPublisher
    {
        public Task PublishAsync(OutboxWorkItem message, CancellationToken ct = default)
            => throw new InvalidOperationException("provider unavailable");
    }
}

