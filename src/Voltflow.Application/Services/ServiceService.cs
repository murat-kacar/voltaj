using Voltflow.Application.Common;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Common;
using Voltflow.Domain.Customers;
using Voltflow.Domain.Services;
using Voltflow.Shared;

namespace Voltflow.Application.Services;

public sealed class ServiceService : IServiceService
{
    private const string CounterKey = "Service";
    private const int MaxTitleLength = 200;
    private const int MaxNotesLength = 2000;
    private const int MaxDescriptionLength = 500;
    private const int MaxUnitLength = 50;
    private const int MaxReasonLength = 500;

    private readonly IServiceRepository _services;
    private readonly ICustomerService _customerService;
    private readonly ICustomerSiteService _siteService;
    private readonly IDocumentNumbers _numbers;
    private readonly IPaymentService _payments;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICommandJournal _commandJournal;
    private readonly IOperationContext _operationContext;
    private readonly TimeProvider _clock;

    public ServiceService(
        IServiceRepository services,
        ICustomerService customerService,
        ICustomerSiteService siteService,
        IDocumentNumbers numbers,
        IPaymentService payments,
        IUnitOfWork unitOfWork,
        ICommandJournal commandJournal,
        IOperationContext operationContext,
        TimeProvider clock)
    {
        _services = services;
        _customerService = customerService;
        _siteService = siteService;
        _numbers = numbers;
        _payments = payments;
        _unitOfWork = unitOfWork;
        _commandJournal = commandJournal;
        _operationContext = operationContext;
        _clock = clock;
    }

    // ---- reading -----------------------------------------------------------------------

    public async Task<Result<ServiceDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var service = await _services.GetByIdAsync(id, ct);
        return service is null ? NotFound() : Result<ServiceDto>.Ok(ServiceDto.MapFrom(service));
    }

    public async Task<Result<PagedResult<ServiceSummaryDto>>> ListAsync(
        string? search = null, string? status = null,
        Guid? customerId = null, Guid? assignedUserId = null,
        int? limit = null, int? offset = null,
        CancellationToken ct = default)
    {
        ServiceStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<ServiceStatus>(status, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
                return Result<PagedResult<ServiceSummaryDto>>.Fail($"Unknown service status: '{status}'.");
            statusFilter = parsed;
        }

        var filter = new ServiceFilter(search, statusFilter?.ToString(), customerId, assignedUserId);
        var page = await _services.ListPagedAsync(
            filter, PaginationDefaults.NormalizeLimit(limit), PaginationDefaults.NormalizeOffset(offset), ct);

        return Result<PagedResult<ServiceSummaryDto>>.Ok(page.Map(ServiceSummaryDto.MapFrom));
    }

    // ---- draft phase -------------------------------------------------------------------

    public async Task<Result<ServiceDto>> CreateDraftAsync(CreateServiceDraftRequest request, CancellationToken ct = default)
    {
        var invalid = ValidateDetails(request.Title, request.Notes);
        if (invalid is not null) return Result<ServiceDto>.Fail(invalid);
        invalid = ReadLines(request.Items, out var lines);
        if (invalid is not null) return Result<ServiceDto>.Fail(invalid);

        var customerResult = await _customerService.GetByIdAsync(request.CustomerId, ct);
        if (!customerResult.IsSuccess) return Result<ServiceDto>.Fail("Customer not found.", "CUSTOMER_NOT_FOUND");
        if (!customerResult.Value.IsActive) return Result<ServiceDto>.Fail("The customer is not active.", "CUSTOMER_INACTIVE");

        var refused = await CheckPlaceAsync(request.CustomerId, request.SiteId, request.AssetId, ct);
        if (refused is not null) return refused;

        var now = Now();
        var number = await _numbers.PrepareAsync(CounterKey, "SRV", ct);

        Service service;
        try
        {
            service = Service.CreateDraft(request.CustomerId, number(), request.Title, request.Notes, request.ValidUntil, request.SiteId, request.AssetId, now);
            foreach (var draft in lines)
            {
                service.AddItem(draft.Description, draft.Quantity, draft.Unit, draft.UnitPrice, draft.VatRate, draft.Kind, now, "Initial draft line");
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            return await RefusedByDomainAsync(exception, ct);
        }

        _commandJournal.MarkResolved(_operationContext.OperationId, success: true, errorCode: null);
        await _services.AddAsync(service, ct);
        await _unitOfWork.CommitAsync(ct);
        return Result<ServiceDto>.Ok(ServiceDto.MapFrom(service));
    }

    public async Task<Result<ServiceDto>> UpdateDraftAsync(Guid id, UpdateServiceDraftRequest request, CancellationToken ct = default)
    {
        var invalid = ValidateDetails(request.Title, request.Notes);
        if (invalid is not null) return Result<ServiceDto>.Fail(invalid);

        var service = await _services.GetByIdAsync(id, ct);
        if (service is null) return NotFound();

        var refused = await CheckPlaceAsync(service.CustomerId, request.SiteId, request.AssetId, ct);
        if (refused is not null) return refused;

        return await ApplyAsync(service, () => service.UpdateDraftDetails(request.Title, request.Notes, request.ValidUntil, request.SiteId, request.AssetId), ct);
    }

    public async Task<Result<ServiceDto>> AddItemAsync(Guid id, ServiceLineRequest request, CancellationToken ct = default)
    {
        var invalid = ReadLine(request, out var line);
        if (invalid is not null) return Result<ServiceDto>.Fail(invalid);
        var auditNote = request.AuditNote ?? "Line added.";

        var service = await _services.GetByIdAsync(id, ct);
        if (service is null) return NotFound();

        var now = Now();
        return await ApplyAsync(service, () => service.AddItem(line!.Description, line.Quantity, line.Unit, line.UnitPrice, line.VatRate, line.Kind, now, auditNote), ct);
    }

    public async Task<Result<ServiceDto>> RemoveItemAsync(Guid id, Guid itemId, string auditNote, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(auditNote)) return Result<ServiceDto>.Fail("Audit note is required for item removal.");
        var service = await _services.GetByIdAsync(id, ct);
        if (service is null) return NotFound();

        var now = Now();
        return await ApplyAsync(service, () => service.RemoveItem(itemId, now, auditNote), ct);
    }

    public async Task<Result<ServiceDto>> ReviseAsync(Guid id, CancellationToken ct = default)
    {
        var source = await _services.GetByIdAsync(id, ct);
        if (source is null) return NotFound();
        
        var nextNumber = await _numbers.PrepareAsync(CounterKey, "SRV", ct);
        Service copy;
        try
        {
            copy = source.CopyAsDraft(nextNumber(), Now());
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            return await RefusedByDomainAsync(exception, ct);
        }

        _commandJournal.MarkResolved(_operationContext.OperationId, success: true, errorCode: null);
        await _services.AddAsync(copy, ct);
        await _unitOfWork.CommitAsync(ct);
        return Result<ServiceDto>.Ok(ServiceDto.MapFrom(copy));
    }

    public async Task<Result> DeleteDraftAsync(Guid id, CancellationToken ct = default)
    {
        var service = await _services.GetByIdAsync(id, ct);
        if (service is null) return Result.Fail("Service not found.", "SERVICE_NOT_FOUND");
        if (service.Status != ServiceStatus.Draft) return Result.Fail("Only a draft service can be deleted.", "SERVICE_NOT_DRAFT");

        _commandJournal.MarkResolved(_operationContext.OperationId, success: true, errorCode: null);
        await _services.DeleteAsync(id, ct);
        await _unitOfWork.CommitAsync(ct);
        return Result.Ok();
    }

    // ---- decisions ---------------------------------------------------------------------

    public async Task<Result<ServiceDto>> IssueAsync(Guid id, CancellationToken ct = default)
    {
        var service = await _services.GetByIdAsync(id, ct);
        if (service is null) return NotFound();

        var now = Now();
        return await ApplyAsync(service, () => service.Issue(now), ct);
    }

    public async Task<Result<ServiceDto>> AcceptAsync(Guid id, AcceptServiceRequest request, CancellationToken ct = default)
    {
        var service = await _services.GetByIdAsync(id, ct);
        if (service is null) return NotFound();

        var now = Now();
        return await ApplyAsync(service, () => service.Accept(request.RequiredDepositPercentage, now), ct);
    }

    public async Task<Result<ServiceDto>> RejectAsync(Guid id, string reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) return Result<ServiceDto>.Fail("Reason is required.");
        var service = await _services.GetByIdAsync(id, ct);
        if (service is null) return NotFound();

        var now = Now();
        var amountToRefund = service.DepositPaidAmount;
        if (amountToRefund > 0)
        {
            var refundResult = await _payments.RefundAsync(service.CustomerId, amountToRefund, "SystemRefund", $"Service {service.Number} rejected: {reason}", ct);
            if (!refundResult.IsSuccess) return Result<ServiceDto>.Fail($"Failed to refund deposit: {refundResult.Error}");
        }

        return await ApplyAsync(service, () => service.Reject(reason, now), ct);
    }

    public async Task<Result<ServiceDto>> CancelAsync(Guid id, string? reason, CancellationToken ct = default)
    {
        var service = await _services.GetByIdAsync(id, ct);
        if (service is null) return NotFound();

        var now = Now();
        var amountToRefund = service.DepositPaidAmount;
        if (amountToRefund > 0)
        {
            var refundResult = await _payments.RefundAsync(service.CustomerId, amountToRefund, "SystemRefund", $"Service {service.Number} cancelled", ct);
            if (!refundResult.IsSuccess) return Result<ServiceDto>.Fail($"Failed to refund deposit: {refundResult.Error}");
        }

        return await ApplyAsync(service, () => service.Cancel(reason, now), ct);
    }

    // ---- active phase operations -------------------------------------------------------

    public async Task<Result<ServiceDto>> AssignAsync(Guid id, AssignServiceRequest request, CancellationToken ct = default)
    {
        var service = await _services.GetByIdAsync(id, ct);
        if (service is null) return NotFound();

        return await ApplyAsync(service, () => service.Assign(request.AssignedUserId, Now()), ct);
    }

    public async Task<Result<ServiceDto>> HoldAsync(Guid id, HoldServiceRequest request, CancellationToken ct = default)
    {
        var service = await _services.GetByIdAsync(id, ct);
        if (service is null) return NotFound();

        return await ApplyAsync(service, () => service.Hold(Now()), ct);
    }

    public async Task<Result<ServiceDto>> ResumeAsync(Guid id, CancellationToken ct = default)
    {
        var service = await _services.GetByIdAsync(id, ct);
        if (service is null) return NotFound();

        return await ApplyAsync(service, () => service.Resume(Now()), ct);
    }

    public async Task<Result<ServiceDto>> CompleteAsync(Guid id, CompleteServiceRequest request, CancellationToken ct = default)
    {
        var service = await _services.GetByIdAsync(id, ct);
        if (service is null) return NotFound();

        return await ApplyAsync(service, () => service.CompleteService(request.FinalAmountOverride, Now()), ct);
    }

    // ---- financial ---------------------------------------------------------------------

    public async Task<Result<ServiceDto>> PayDepositAsync(Guid id, PayServiceDepositRequest request, CancellationToken ct = default)
    {
        var amount = Money.Round(request.Amount);
        if (amount <= 0) return Result<ServiceDto>.Fail("Amount must be at least one cent.");

        var service = await _services.GetByIdAsync(id, ct);
        if (service is null) return NotFound();

        var paymentResult = await _payments.CreateAsync(new CreatePaymentRequest(service.CustomerId, amount, request.PaymentMethod, DateOnly.FromDateTime(Now())), ct);
        if (!paymentResult.IsSuccess) return Result<ServiceDto>.Fail(paymentResult.Error);

        return await ApplyAsync(service, () => service.PayDeposit(amount, Now()), ct);
    }

    public async Task<Result<ServiceDto>> IssuePartialInvoiceAsync(Guid id, IssuePartialInvoiceRequest request, CancellationToken ct = default)
    {
        var amount = Money.Round(request.Amount);
        var service = await _services.GetByIdAsync(id, ct);
        if (service is null) return NotFound();

        return await ApplyAsync(service, () => service.RecordPartialInvoice(amount, Now()), ct);
    }

    public async Task<Result<ServiceDto>> UpdateSubStatusAsync(Guid id, UpdateSubStatusRequest request, CancellationToken ct = default)
    {
        var service = await _services.GetByIdAsync(id, ct);
        if (service is null) return NotFound();

        return await ApplyAsync(service, () => service.UpdateSubStatus(request.SubStatus, Now()), ct);
    }

    // ---- helpers -----------------------------------------------------------------------

    private async Task<Result<ServiceDto>> ApplyAsync(Service service, Action change, CancellationToken ct)
    {
        try
        {
            change();
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            return await RefusedByDomainAsync(exception, ct);
        }

        _commandJournal.MarkResolved(_operationContext.OperationId, success: true, errorCode: null);
        await _unitOfWork.CommitAsync(ct);
        return Result<ServiceDto>.Ok(ServiceDto.MapFrom(service));
    }

    private async Task<Result<ServiceDto>> RefusedByDomainAsync(Exception exception, CancellationToken ct)
    {
        await _commandJournal.ResolveNowAsync(_operationContext.OperationId, success: false, "DOMAIN_VALIDATION_FAILED", ct);
        return Result<ServiceDto>.Fail(exception.Message);
    }

    private async Task<Result<ServiceDto>?> CheckPlaceAsync(Guid customerId, Guid? siteId, Guid? assetId, CancellationToken ct)
    {
        if (siteId is null)
        {
            if (assetId is not null) return Result<ServiceDto>.Fail("An asset cannot be selected without a site.");
            return null;
        }

        var siteResult = await _siteService.GetSiteAsync(customerId, siteId.Value, ct);
        if (!siteResult.IsSuccess) return Result<ServiceDto>.Fail("Address not found.", "SITE_NOT_FOUND");
        if (assetId is { } assetIdVal)
        {
            var assetResult = await _siteService.GetAssetAsync(siteId.Value, assetIdVal, ct);
            if (!assetResult.IsSuccess) return Result<ServiceDto>.Fail("Device not found.", "ASSET_NOT_FOUND");
        }
        return null;
    }

    private DateTime Now() => _clock.GetUtcNow().UtcDateTime;

    private static Result<ServiceDto> NotFound() => Result<ServiceDto>.Fail("Service not found.", "SERVICE_NOT_FOUND");

    private static string? ValidateDetails(string? title, string? notes)
    {
        if (string.IsNullOrWhiteSpace(title)) return "Title is required.";
        if (title.Trim().Length > MaxTitleLength) return $"Title can be at most {MaxTitleLength} characters.";
        if (notes is not null && notes.Trim().Length > MaxNotesLength) return $"Notes can be at most {MaxNotesLength} characters.";
        return null;
    }

    private sealed record ServiceItemDraft(string Kind, string Description, decimal Quantity, decimal UnitPrice, string Unit, decimal VatRate);

    private static string? ReadLines(IReadOnlyList<ServiceLineRequest>? requests, out List<ServiceItemDraft> lines)
    {
        lines = [];
        if (requests is null) return null;
        if (requests.Count > Service.MaxItems) return $"A service can have at most {Service.MaxItems} lines.";

        for (var index = 0; index < requests.Count; index++)
        {
            if (requests[index] is null) return $"Line {index + 1} is empty.";
            var invalid = ReadLine(requests[index], out var line);
            if (invalid is not null) return $"Line {index + 1}: {invalid}";
            lines.Add(line!);
        }

        return null;
    }

    private static string? ReadLine(ServiceLineRequest request, out ServiceItemDraft? line)
    {
        line = null;
        if (string.IsNullOrWhiteSpace(request.Description)) return "Description is required.";
        if (request.Description.Trim().Length > MaxDescriptionLength) return $"Description can be at most {MaxDescriptionLength} characters.";
        if (request.Quantity <= 0 || request.Quantity > ServiceItem.MaxQuantity || Math.Round(request.Quantity, 2) != request.Quantity)
            return $"Quantity must be greater than zero and at most {ServiceItem.MaxQuantity:0}, with at most two decimals.";
        if (request.UnitPrice < 0 || request.UnitPrice > ServiceItem.MaxUnitPrice) return $"UnitPrice must be between 0 and {ServiceItem.MaxUnitPrice:0}.";
        
        var vatRate = request.VatRate ?? ServiceItem.DefaultVatRate;
        if (vatRate is < 0 or > 100) return "VatRate must be between 0 and 100.";
        
        var unit = string.IsNullOrWhiteSpace(request.Unit) ? "adet" : request.Unit.Trim();
        if (unit.Length > MaxUnitLength) return $"Unit can be at most {MaxUnitLength} characters.";

        var kind = string.IsNullOrWhiteSpace(request.Kind) ? "Service" : request.Kind.Trim();

        line = new ServiceItemDraft(kind, request.Description, request.Quantity, request.UnitPrice, unit, vatRate);
        return null;
    }
}
