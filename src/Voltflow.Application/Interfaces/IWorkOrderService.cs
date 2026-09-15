using Voltflow.Application.Dtos;
using Voltflow.Shared;

namespace Voltflow.Application.Interfaces;

public interface IWorkOrderService
{
    Task<Result<WorkOrderDto>> CreateAsync(CreateWorkOrderRequest request, CancellationToken ct = default);
    Task<Result<WorkOrderDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<WorkOrderDto>>> ListAsync(CancellationToken ct = default);
    Task<Result<WorkOrderDto>> AssignAsync(Guid id, Guid employeeUserId, CancellationToken ct = default);
    Task<Result<WorkOrderDto>> MarkAsEnRouteAsync(Guid id, CancellationToken ct = default);
    Task<Result<WorkOrderDto>> ReportNoShowAsync(Guid id, ReportNoShowRequest request, CancellationToken ct = default);
    Task<Result<WorkOrderDto>> CompleteSafetyChecklistAsync(Guid id, CancellationToken ct = default);
    Task<Result<WorkOrderDto>> StartAsync(Guid id, StartWorkOrderRequest request, CancellationToken ct = default);
    Task<Result<WorkOrderDto>> CheckInAsync(Guid id, CheckInWorkOrderRequest? request = null, CancellationToken ct = default);
    Task<Result<WorkOrderDto>> CheckOutAsync(Guid id, CheckOutWorkOrderRequest? request = null, CancellationToken ct = default);
    Task<Result<WorkOrderDto>> PutOnHoldAsync(Guid id, HoldWorkOrderRequest request, CancellationToken ct = default);
    Task<Result<WorkOrderDto>> ResumeAsync(Guid id, CancellationToken ct = default);
    Task<Result<WorkOrderDto>> CancelAsync(Guid id, CancelWorkOrderRequest request, CancellationToken ct = default);
    Task<Result<WorkOrderDto>> CompleteAsync(Guid id, CompleteWorkOrderRequest request, CancellationToken ct = default);
    Task<Result<WorkOrderDto>> ApproveForBillingAsync(Guid id, CancellationToken ct = default);
    Task<Result<WorkOrderDto>> InvoiceAsync(Guid id, CancellationToken ct = default);
    Task<Result<WorkOrderDto>> AddMaterialAsync(Guid id, AddMaterialToWorkOrderRequest request, CancellationToken ct = default);
}
