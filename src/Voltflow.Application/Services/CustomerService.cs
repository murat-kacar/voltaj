using Voltflow.Application.Common;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Customers;
using Voltflow.Shared;

namespace Voltflow.Application.Services;

public sealed class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ICommandJournal _commandJournal;
    private readonly IOperationContext _operationContext;

    public CustomerService(ICustomerRepository customerRepository, ICommandJournal commandJournal, IOperationContext operationContext)
    {
        _customerRepository = customerRepository;
        _commandJournal = commandJournal;
        _operationContext = operationContext;
    }

    public async Task<Result<CustomerDto>> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.FullName)) return Result<CustomerDto>.Fail("FullName is required.");
        if (string.IsNullOrWhiteSpace(request.Email)) return Result<CustomerDto>.Fail("Email is required.");

        var existing = await _customerRepository.GetByEmailAsync(request.Email, ct);
        if (existing is not null) return Result<CustomerDto>.Fail("Customer already exists.");

        var customer = new Customer(request.FullName, request.Email, request.Phone, request.TaxNumber ?? string.Empty);
        _commandJournal.MarkResolved(_operationContext.OperationId, success: true, errorCode: null);
        await _customerRepository.AddAsync(customer, ct);

        return Result<CustomerDto>.Ok(MapCustomer(customer));
    }

    public async Task<Result<CustomerDto>> ConvertToActiveAsync(Guid customerId, CancellationToken ct = default)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, ct);
        if (customer is null) return Result<CustomerDto>.Fail("Customer not found.");

        try
        {
            customer.ConvertToActive();
            _commandJournal.MarkResolved(_operationContext.OperationId, success: true, errorCode: null);
            await _customerRepository.UpdateAsync(customer, ct);
            return Result<CustomerDto>.Ok(MapCustomer(customer));
        }
        catch (InvalidOperationException exception)
        {
            await _commandJournal.ResolveNowAsync(_operationContext.OperationId, success: false, "DOMAIN_VALIDATION_FAILED", ct);
            return Result<CustomerDto>.Fail(exception.Message);
        }
    }

    public async Task<Result<CustomerDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var customer = await _customerRepository.GetByIdAsync(id, ct);
        if (customer is null) return Result<CustomerDto>.Fail("Customer not found.");

        return Result<CustomerDto>.Ok(MapCustomer(customer));
    }

    public async Task<Result<PagedResult<CustomerDto>>> ListAsync(int? limit = null, int? offset = null, CancellationToken ct = default)
    {
        var page = await _customerRepository.ListPagedAsync(
            PaginationDefaults.NormalizeLimit(limit), PaginationDefaults.NormalizeOffset(offset), ct);
        return Result<PagedResult<CustomerDto>>.Ok(page.Map(MapCustomer));
    }

    private static CustomerDto MapCustomer(Customer customer) =>
        new(customer.Id, customer.FullName, customer.Email, customer.Phone, customer.TaxNumber, customer.Type.ToString(), customer.IsActive, customer.CreatedAt);
}
