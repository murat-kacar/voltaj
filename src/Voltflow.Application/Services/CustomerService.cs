using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Customers;
using Voltflow.Shared;

namespace Voltflow.Application.Services;

public sealed class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;

    public CustomerService(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<Result<CustomerDto>> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.FullName)) return Result<CustomerDto>.Fail("FullName is required.");
        if (string.IsNullOrWhiteSpace(request.Email)) return Result<CustomerDto>.Fail("Email is required.");

        var existing = await _customerRepository.GetByEmailAsync(request.Email, ct);
        if (existing is not null) return Result<CustomerDto>.Fail("Customer already exists.");

        var customer = new Customer(request.FullName, request.Email, request.Phone, request.TaxNumber ?? string.Empty);
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
            await _customerRepository.UpdateAsync(customer, ct);
            return Result<CustomerDto>.Ok(MapCustomer(customer));
        }
        catch (InvalidOperationException exception)
        {
            return Result<CustomerDto>.Fail(exception.Message);
        }
    }

    public async Task<Result<CustomerDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var customer = await _customerRepository.GetByIdAsync(id, ct);
        if (customer is null) return Result<CustomerDto>.Fail("Customer not found.");

        return Result<CustomerDto>.Ok(MapCustomer(customer));
    }

    public async Task<Result<IReadOnlyList<CustomerDto>>> ListAsync(CancellationToken ct = default)
    {
        var customers = await _customerRepository.ListAsync(ct);
        var dtos = customers.Select(MapCustomer).ToList();
        return Result<IReadOnlyList<CustomerDto>>.Ok(dtos);
    }

    private static CustomerDto MapCustomer(Customer customer) =>
        new(customer.Id, customer.FullName, customer.Email, customer.Phone, customer.TaxNumber, customer.Type.ToString(), customer.IsActive, customer.CreatedAt);
}
