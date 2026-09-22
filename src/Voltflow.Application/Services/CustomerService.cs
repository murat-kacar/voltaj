using System.Net.Mail;
using Voltflow.Application.Common;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Customers;
using Voltflow.Shared;

namespace Voltflow.Application.Services;

public sealed class CustomerService : ICustomerService
{
    private const int MaxNameLength = 200;
    private const int MaxEmailLength = 200;
    private const int MaxPhoneLength = 40;
    private const int MaxTaxNumberLength = 32;

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
        var invalid = Validate(request.FullName, request.Email, request.Phone, request.TaxNumber);
        if (invalid is not null) return Result<CustomerDto>.Fail(invalid);

        var duplicate = await FindDuplicateAsync(request.Email, request.TaxNumber, exceptId: null, ct);
        if (duplicate is not null) return duplicate;

        var customer = new Customer(request.FullName, request.Email, request.Phone, request.TaxNumber);
        _commandJournal.MarkResolved(_operationContext.OperationId, success: true, errorCode: null);
        await _customerRepository.AddAsync(customer, ct);

        return Result<CustomerDto>.Ok(MapCustomer(customer));
    }

    public async Task<Result<CustomerDto>> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken ct = default)
    {
        var invalid = Validate(request.FullName, request.Email, request.Phone, request.TaxNumber);
        if (invalid is not null) return Result<CustomerDto>.Fail(invalid);

        var customer = await _customerRepository.GetByIdAsync(id, ct);
        if (customer is null) return NotFound();

        var duplicate = await FindDuplicateAsync(request.Email, request.TaxNumber, exceptId: id, ct);
        if (duplicate is not null) return duplicate;

        customer.Update(request.FullName, request.Email, request.Phone, request.TaxNumber);
        _commandJournal.MarkResolved(_operationContext.OperationId, success: true, errorCode: null);
        await _customerRepository.UpdateAsync(customer, ct);

        return Result<CustomerDto>.Ok(MapCustomer(customer));
    }

    public async Task<Result<CustomerDto>> ConvertToActiveAsync(Guid customerId, CancellationToken ct = default)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, ct);
        if (customer is null) return Result<CustomerDto>.Fail("Customer not found.", "CUSTOMER_NOT_FOUND");

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

    public async Task<Result<CustomerDto>> SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var customer = await _customerRepository.GetByIdAsync(id, ct);
        if (customer is null) return NotFound();
        if (customer.IsActive == isActive) return Result<CustomerDto>.Ok(MapCustomer(customer));

        if (isActive) customer.SetActive();
        else customer.SetInactive();
        _commandJournal.MarkResolved(_operationContext.OperationId, success: true, errorCode: null);
        await _customerRepository.UpdateAsync(customer, ct);

        return Result<CustomerDto>.Ok(MapCustomer(customer));
    }

    public async Task<Result<CustomerDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var customer = await _customerRepository.GetByIdAsync(id, ct);
        if (customer is null) return NotFound();

        return Result<CustomerDto>.Ok(MapCustomer(customer));
    }

    public async Task<Result<IReadOnlyDictionary<Guid, string>>> GetNamesAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
    {
        var names = await _customerRepository.GetNamesAsync(ids, ct);
        return Result<IReadOnlyDictionary<Guid, string>>.Ok(names);
    }

    public async Task<Result<PagedResult<CustomerDto>>> ListAsync(
        string? search = null, string? type = null, bool? active = null, int? limit = null, int? offset = null, CancellationToken ct = default)
    {
        CustomerType? typeFilter = null;
        if (!string.IsNullOrWhiteSpace(type))
        {
            if (!Enum.TryParse<CustomerType>(type, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
                return Result<PagedResult<CustomerDto>>.Fail("Unknown customer type.");
            typeFilter = parsed;
        }

        var page = await _customerRepository.ListPagedAsync(
            new CustomerFilter(search, typeFilter, active), PaginationDefaults.NormalizeLimit(limit), PaginationDefaults.NormalizeOffset(offset), ct);
        return Result<PagedResult<CustomerDto>>.Ok(page.Map(MapCustomer));
    }

    private static Result<CustomerDto> NotFound() => Result<CustomerDto>.Fail("Customer not found.", "CUSTOMER_NOT_FOUND");

    /// <summary>The rules a customer's details must satisfy, or the first one they break.</summary>
    private static string? Validate(string? fullName, string? email, string? phone, string? taxNumber)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "FullName is required.";
        if (fullName.Trim().Length > MaxNameLength) return $"FullName can be at most {MaxNameLength} characters.";
        if (string.IsNullOrWhiteSpace(phone)) return "Phone is required.";
        if (phone.Trim().Length > MaxPhoneLength) return $"Phone can be at most {MaxPhoneLength} characters.";
        if (!string.IsNullOrWhiteSpace(email))
        {
            var cleanEmail = email.Trim();
            if (cleanEmail.Length > MaxEmailLength) return $"Email can be at most {MaxEmailLength} characters.";
            if (!MailAddress.TryCreate(cleanEmail, out var address) || address.Address != cleanEmail) return "Email is not a valid address.";
        }

        if (taxNumber is not null && taxNumber.Trim().Length > MaxTaxNumberLength) return $"TaxNumber can be at most {MaxTaxNumberLength} characters.";
        return null;
    }

    /// <summary>An email address or a tax number belongs to one customer. Blank ones do not count: many customers have neither.</summary>
    private async Task<Result<CustomerDto>?> FindDuplicateAsync(string? email, string? taxNumber, Guid? exceptId, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(email))
        {
            var byEmail = await _customerRepository.GetByEmailAsync(email.Trim(), ct);
            if (byEmail is not null && byEmail.Id != exceptId)
                return Result<CustomerDto>.Fail("A customer with this email already exists.", "CUSTOMER_EMAIL_EXISTS");
        }

        if (!string.IsNullOrWhiteSpace(taxNumber))
        {
            var byTaxNumber = await _customerRepository.GetByTaxNumberAsync(taxNumber.Trim(), ct);
            if (byTaxNumber is not null && byTaxNumber.Id != exceptId)
                return Result<CustomerDto>.Fail("A customer with this tax number already exists.", "CUSTOMER_TAXNUMBER_EXISTS");
        }

        return null;
    }

    private static CustomerDto MapCustomer(Customer customer) =>
        new(customer.Id, customer.FullName, customer.Email, customer.Phone, customer.TaxNumber, customer.Type.ToString(), customer.IsActive, customer.CreatedAt);
}
