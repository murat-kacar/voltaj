using Voltflow.Application.Common;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Finance;
using Voltflow.Domain.WorkOrders;
using Voltflow.Shared;

namespace Voltflow.Application.Services;

public sealed class WorkOrderService : IWorkOrderService
{
    private readonly IWorkOrderRepository _repository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly ICurrentUser _currentUser;
    private readonly ICommandJournal _commandJournal;
    private readonly IOperationContext _operationContext;
    private readonly IPaymentRepository _paymentRepository;

    public WorkOrderService(
        IWorkOrderRepository repository,
        IOutboxRepository outboxRepository,
        ICurrentUser currentUser,
        ICommandJournal commandJournal,
        IOperationContext operationContext,
        IPaymentRepository paymentRepository)
    {
        _repository = repository;
        _outboxRepository = outboxRepository;
        _currentUser = currentUser;
        _commandJournal = commandJournal;
        _operationContext = operationContext;
        _paymentRepository = paymentRepository;
    }

    public async Task<Result<WorkOrderDto>> CreateAsync(CreateWorkOrderRequest request, CancellationToken ct = default)
    {
        if (request.CustomerId == Guid.Empty) return Result<WorkOrderDto>.Fail("CustomerId is required.");
        if (string.IsNullOrWhiteSpace(request.Title)) return Result<WorkOrderDto>.Fail("Title is required.");
        var order = new WorkOrder(request.CustomerId, request.Title.Trim());
        _commandJournal.MarkResolved(_operationContext.OperationId, success: true, errorCode: null);
        await _repository.AddAsync(order, ct);
        return Result<WorkOrderDto>.Ok(Map(order));
    }

    public async Task<Result<WorkOrderDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var order = await _repository.GetByIdAsync(id, ct);
        if (order is not null && IsTechnicianOnly() && order.AssignedUserId != _currentUser.UserId) order = null;
        return order is null ? Result<WorkOrderDto>.Fail("Work order not found.") : Result<WorkOrderDto>.Ok(Map(order));
    }

    public async Task<Result<PagedResult<WorkOrderDto>>> ListAsync(int? limit = null, int? offset = null, CancellationToken ct = default)
    {
        var assignedFilter = IsTechnicianOnly() && _currentUser.UserId is Guid userId ? userId : (Guid?)null;
        var page = await _repository.ListPagedAsync(
            PaginationDefaults.NormalizeLimit(limit), PaginationDefaults.NormalizeOffset(offset), assignedFilter, ct);
        return Result<PagedResult<WorkOrderDto>>.Ok(page.Map(Map));
    }

    public Task<Result<WorkOrderDto>> AssignAsync(Guid id, Guid employeeUserId, CancellationToken ct = default)
        => ExecuteActionAsync(id, order => order.Assign(employeeUserId), ct);

    public Task<Result<WorkOrderDto>> MarkAsEnRouteAsync(Guid id, CancellationToken ct = default)
        => ExecuteActionAsync(id, order => order.MarkAsEnRoute(), ct);

    public Task<Result<WorkOrderDto>> ReportNoShowAsync(Guid id, ReportNoShowRequest request, CancellationToken ct = default)
        => ExecuteActionAsync(id, order => order.ReportNoShow(request.Reason), ct);

    public Task<Result<WorkOrderDto>> CompleteSafetyChecklistAsync(Guid id, CancellationToken ct = default)
        => ExecuteActionAsync(id, order => order.CompleteSafetyChecklist(), ct);

    public Task<Result<WorkOrderDto>> StartAsync(Guid id, StartWorkOrderRequest request, CancellationToken ct = default)
        => ExecuteActionAsync(id, order => order.Start(request.TargetCompletionDate), ct);

    public Task<Result<WorkOrderDto>> CheckInAsync(Guid id, CheckInWorkOrderRequest? request = null, CancellationToken ct = default)
        => ExecuteActionAsync(id, async order =>
        {
            order.CheckIn(request?.Notes);
            var payload = System.Text.Json.JsonSerializer.Serialize(new { order.Id, order.CustomerId, order.Number, order.Title });
            await _outboxRepository.QueueAsync(new OutboxWorkItem(Guid.Empty, "TechnicianEnRoute", payload, 0), ct);
        }, ct);

    public Task<Result<WorkOrderDto>> CheckOutAsync(Guid id, CheckOutWorkOrderRequest? request = null, CancellationToken ct = default)
        => ExecuteActionAsync(id, order => order.CheckOut(request?.Notes), ct);

    public Task<Result<WorkOrderDto>> PutOnHoldAsync(Guid id, HoldWorkOrderRequest request, CancellationToken ct = default)
        => ExecuteActionAsync(id, order => order.PutOnHold(request.Reason), ct);

    public Task<Result<WorkOrderDto>> ResumeAsync(Guid id, CancellationToken ct = default)
        => ExecuteActionAsync(id, order => order.Resume(), ct);

    public Task<Result<WorkOrderDto>> CancelAsync(Guid id, CancelWorkOrderRequest request, CancellationToken ct = default)
        => ExecuteActionAsync(id, order => order.Cancel(request.Reason), ct);

    public Task<Result<WorkOrderDto>> CompleteAsync(Guid id, CompleteWorkOrderRequest request, CancellationToken ct = default)
        => ExecuteActionAsync(id, async order =>
        {
            order.Complete(request.SignatureData, request.ProofOfWorkPhotoUrl);
            var payload = System.Text.Json.JsonSerializer.Serialize(new { order.Id, order.CustomerId, order.Number, order.Title });
            await _outboxRepository.QueueAsync(new OutboxWorkItem(Guid.Empty, "WorkOrderCompleted", payload, 0), ct);
        }, ct);

    public Task<Result<WorkOrderDto>> ApproveForBillingAsync(Guid id, CancellationToken ct = default)
        => ExecuteActionAsync(id, order => order.ApproveForBilling(), ct);

    public Task<Result<WorkOrderDto>> InvoiceAsync(Guid id, CancellationToken ct = default)
        => ExecuteActionAsync(id, order =>
        {
            order.Invoice();
            if (order.Total > 0)
            {
                var invoice = new SalesInvoice(order.CustomerId, $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}", order.Total, DateOnly.FromDateTime(DateTime.UtcNow));
                _paymentRepository.StageInvoice(invoice);
            }
            return Task.CompletedTask;
        }, ct);

    public Task<Result<WorkOrderDto>> AddMaterialAsync(Guid id, AddMaterialToWorkOrderRequest request, CancellationToken ct = default)
        => ExecuteActionAsync(id, order => order.AddItem(request.Description, request.Quantity, request.UnitPrice), ct);

    private async Task<Result<WorkOrderDto>> ExecuteActionAsync(Guid id, Action<WorkOrder> action, CancellationToken ct)
    {
        return await ExecuteActionAsync(id, order =>
        {
            action(order);
            return Task.CompletedTask;
        }, ct);
    }

    private async Task<Result<WorkOrderDto>> ExecuteActionAsync(Guid id, Func<WorkOrder, Task> actionAsync, CancellationToken ct)
    {
        var order = await _repository.GetByIdAsync(id, ct);
        if (order is null) return Result<WorkOrderDto>.Fail("Work order not found.");
        if (IsTechnicianOnly() && order.AssignedUserId != _currentUser.UserId)
            return Result<WorkOrderDto>.Fail("You are not assigned to this work order.");

        try
        {
            await actionAsync(order);
            // H9: piggyback the Command's resolution onto this same SaveChanges (H7's transaction) -
            // MarkResolved only updates the tracked entity, UpdateAsync below is what actually saves.
            _commandJournal.MarkResolved(_operationContext.OperationId, success: true, errorCode: null);
            await _repository.UpdateAsync(order, ct);
            return Result<WorkOrderDto>.Ok(Map(order));
        }
        catch (InvalidOperationException ex)
        {
            await _commandJournal.ResolveNowAsync(_operationContext.OperationId, success: false, errorCode: "DOMAIN_VALIDATION_FAILED", ct);
            return Result<WorkOrderDto>.Fail(ex.Message);
        }
    }

    private static WorkOrderDto Map(WorkOrder order) => WorkOrderDto.MapFrom(order);
        
    private bool IsTechnicianOnly() => _currentUser.Roles.Contains("Technician") && !_currentUser.Roles.Any(role => role is "Admin" or "Manager");
}
