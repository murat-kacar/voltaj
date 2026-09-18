using Voltflow.Application.Common;
using Voltflow.Application.Dtos;
using Voltflow.Shared;

namespace Voltflow.Application.Interfaces;

public interface ICustomerService
{
    Task<Result<CustomerDto>> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default);
    Task<Result<CustomerDto>> ConvertToActiveAsync(Guid customerId, CancellationToken ct = default);
    Task<Result<CustomerDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<PagedResult<CustomerDto>>> ListAsync(int? limit = null, int? offset = null, CancellationToken ct = default);
}
