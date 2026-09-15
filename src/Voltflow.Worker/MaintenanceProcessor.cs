using Microsoft.Extensions.Logging;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.WorkOrders;

namespace Voltflow.Worker;

public sealed class MaintenanceProcessor
{
    private readonly IMaintenanceContractRepository _contractRepository;
    private readonly IWorkOrderRepository _workOrderRepository;
    private readonly ILogger<MaintenanceProcessor> _logger;

    public MaintenanceProcessor(
        IMaintenanceContractRepository contractRepository,
        IWorkOrderRepository workOrderRepository,
        ILogger<MaintenanceProcessor> logger)
    {
        _contractRepository = contractRepository;
        _workOrderRepository = workOrderRepository;
        _logger = logger;
    }

    public async Task<int> ProcessDueContractsAsync(DateTime currentDate, CancellationToken ct = default)
    {
        var dueContracts = await _contractRepository.GetDueContractsAsync(currentDate, ct);
        int count = 0;

        foreach (var contract in dueContracts)
        {
            var workOrder = new WorkOrder(contract.CustomerId, $"[Periyodik Bakım] {contract.Title}");
            
            await _workOrderRepository.AddAsync(workOrder, ct);
            
            contract.GenerateWorkOrderCompleted();
            await _contractRepository.UpdateAsync(contract, ct);
            
            _logger.LogInformation("Generated maintenance work order {WorkOrderId} for contract {ContractId}", workOrder.Id, contract.Id);
            count++;
        }

        return count;
    }
}
