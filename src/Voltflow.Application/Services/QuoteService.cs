using Voltflow.Application.Common;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Common;
using Voltflow.Domain.Customers;
using Voltflow.Domain.Quotes;
using Voltflow.Shared;

namespace Voltflow.Application.Services;

/// <summary>
/// Quotes: a draft is built and priced, issued to the customer, and then accepted, rejected or left to expire. Everything is checked
/// before anything changes, and the number of a quote is taken only once all of it is valid.
/// </summary>
public sealed class QuoteService : IQuoteService
{
    private const string CounterKey = "quote";
    private const int MaxTitleLength = 200;
    private const int MaxNotesLength = 2000;
    private const int MaxDescriptionLength = 500;
    private const int MaxUnitLength = 16;
    private const int MaxReasonLength = 500;

    private readonly IQuoteRepository _quotes;
    private readonly ICustomerRepository _customers;
    private readonly ICustomerSiteRepository _sites;
    private readonly IDocumentNumbers _numbers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICommandJournal _commandJournal;
    private readonly IOperationContext _operationContext;
    private readonly TimeProvider _clock;

    public QuoteService(
        IQuoteRepository quotes,
        ICustomerRepository customers,
        ICustomerSiteRepository sites,
        IDocumentNumbers numbers,
        IUnitOfWork unitOfWork,
        ICommandJournal commandJournal,
        IOperationContext operationContext,
        TimeProvider clock)
    {
        _quotes = quotes;
        _customers = customers;
        _sites = sites;
        _numbers = numbers;
        _unitOfWork = unitOfWork;
        _commandJournal = commandJournal;
        _operationContext = operationContext;
        _clock = clock;
    }

    // ---- reading -----------------------------------------------------------------------------------------

    public async Task<Result<PagedResult<QuoteSummaryDto>>> ListAsync(
        string? search = null, string? state = null, Guid? customerId = null, int? limit = null, int? offset = null, CancellationToken ct = default)
    {
        QuoteState? stateFilter = null;
        if (!string.IsNullOrWhiteSpace(state))
        {
            if (!Enum.TryParse<QuoteState>(state, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
                return Result<PagedResult<QuoteSummaryDto>>.Fail("Unknown quote state.");
            stateFilter = parsed;
        }

        var page = await _quotes.ListPagedAsync(
            new QuoteFilter(search, stateFilter, customerId), PaginationDefaults.NormalizeLimit(limit), PaginationDefaults.NormalizeOffset(offset), ct);
        var names = await _customers.GetNamesAsync(page.Items.Select(quote => quote.CustomerId).Distinct().ToList(), ct);
        return Result<PagedResult<QuoteSummaryDto>>.Ok(page.Map(quote => MapSummary(quote, names)));
    }

    public async Task<Result<QuoteDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var quote = await _quotes.GetByIdAsync(id, ct);
        if (quote is null) return NotFound();

        return Result<QuoteDto>.Ok(await MapAsync(quote, customer: null, ct));
    }

    // ---- the draft ---------------------------------------------------------------------------------------

    public async Task<Result<QuoteDto>> CreateAsync(CreateQuoteRequest request, CancellationToken ct = default)
    {
        if (request.CustomerId == Guid.Empty) return Result<QuoteDto>.Fail("CustomerId is required.");
        var invalid = ValidateDetails(request.Title, request.Notes);
        if (invalid is not null) return Result<QuoteDto>.Fail(invalid);
        invalid = ReadLines(request.Items, out var lines);
        if (invalid is not null) return Result<QuoteDto>.Fail(invalid);
        if (IsPast(request.ValidUntil)) return ValidityPast();

        var customer = await _customers.GetByIdAsync(request.CustomerId, ct);
        if (customer is null) return Result<QuoteDto>.Fail("Customer not found.", "CUSTOMER_NOT_FOUND");
        if (!customer.IsActive) return Result<QuoteDto>.Fail("The customer is not active.", "CUSTOMER_INACTIVE");
        var refused = await CheckPlaceAsync(customer.Id, request.SiteId, request.AssetId, ct);
        if (refused is not null) return refused;

        var nextNumber = await _numbers.PrepareAsync(CounterKey, "TK", ct);
        Quote quote;
        try
        {
            quote = Quote.Create(nextNumber, customer.Id, request.Title, request.Notes, request.ValidUntil, request.SiteId, request.AssetId, lines);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            return await RefusedByDomainAsync(exception, ct);
        }

        _commandJournal.MarkResolved(_operationContext.OperationId, success: true, errorCode: null);
        await _quotes.AddAsync(quote, ct);
        return Result<QuoteDto>.Ok(await MapAsync(quote, customer, ct));
    }

    public async Task<Result<QuoteDto>> UpdateAsync(Guid id, UpdateQuoteRequest request, CancellationToken ct = default)
    {
        var invalid = ValidateDetails(request.Title, request.Notes);
        if (invalid is not null) return Result<QuoteDto>.Fail(invalid);
        invalid = ReadLines(request.Items, out var lines);
        if (invalid is not null) return Result<QuoteDto>.Fail(invalid);
        if (IsPast(request.ValidUntil)) return ValidityPast();

        var quote = await _quotes.GetByIdAsync(id, ct);
        if (quote is null) return NotFound();
        if (quote.State != QuoteState.Draft) return NotDraft();
        var refused = await CheckPlaceAsync(quote.CustomerId, request.SiteId, request.AssetId, ct);
        if (refused is not null) return refused;

        return await ApplyAsync(quote, () => quote.Update(request.Title, request.Notes, request.ValidUntil, request.SiteId, request.AssetId, lines), ct);
    }

    public async Task<Result<QuoteDto>> AddItemAsync(Guid id, QuoteLineRequest request, CancellationToken ct = default)
    {
        var invalid = ReadLine(request, out var line);
        if (invalid is not null) return Result<QuoteDto>.Fail(invalid);

        var quote = await _quotes.GetByIdAsync(id, ct);
        if (quote is null) return NotFound();
        if (quote.State != QuoteState.Draft) return NotDraft();
        if (quote.Items.Count >= Quote.MaxItems) return Result<QuoteDto>.Fail($"A quote can have at most {Quote.MaxItems} lines.");

        return await ApplyAsync(quote, () => quote.AddItem(line!.Description, line.Quantity, line.UnitPrice, line.Unit, line.VatRate, line.Kind), ct);
    }

    /// <summary>How an issued quote is revised: a new draft with the same customer, address, notes and lines.</summary>
    public async Task<Result<QuoteDto>> CopyAsync(Guid id, CancellationToken ct = default)
    {
        var source = await _quotes.GetByIdAsync(id, ct);
        if (source is null) return NotFound();
        var customer = await _customers.GetByIdAsync(source.CustomerId, ct);
        if (customer is null) return Result<QuoteDto>.Fail("Customer not found.", "CUSTOMER_NOT_FOUND");
        if (!customer.IsActive) return Result<QuoteDto>.Fail("The customer is not active.", "CUSTOMER_INACTIVE");

        var nextNumber = await _numbers.PrepareAsync(CounterKey, "TK", ct);
        Quote copy;
        try
        {
            copy = source.CopyAsDraft(nextNumber);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            return await RefusedByDomainAsync(exception, ct);
        }

        _commandJournal.MarkResolved(_operationContext.OperationId, success: true, errorCode: null);
        await _quotes.AddAsync(copy, ct);
        return Result<QuoteDto>.Ok(await MapAsync(copy, customer, ct));
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var quote = await _quotes.GetByIdAsync(id, ct);
        if (quote is null) return Result.Fail("Quote not found.", "QUOTE_NOT_FOUND");
        if (quote.State != QuoteState.Draft) return Result.Fail("Only a draft quote can be deleted.", "QUOTE_NOT_DRAFT");

        _commandJournal.MarkResolved(_operationContext.OperationId, success: true, errorCode: null);
        await _quotes.DeleteAsync(id, ct);
        return Result.Ok();
    }

    // ---- the decision ------------------------------------------------------------------------------------

    public async Task<Result<QuoteDto>> IssueAsync(Guid id, CancellationToken ct = default)
    {
        var quote = await _quotes.GetByIdAsync(id, ct);
        if (quote is null) return NotFound();
        if (quote.State != QuoteState.Draft) return Result<QuoteDto>.Fail("Only draft quotes can be issued.", "QUOTE_NOT_DRAFT");
        if (quote.Items.Count == 0) return Result<QuoteDto>.Fail("A quote must contain at least one item before it can be issued.", "QUOTE_EMPTY");
        if (IsPast(quote.ValidUntil)) return ValidityPast();

        var now = Now();
        return await ApplyAsync(quote, () => quote.Issue(now), ct);
    }

    public async Task<Result<QuoteDto>> AcceptAsync(Guid id, AcceptQuoteRequest request, CancellationToken ct = default)
    {
        if (request.RequiredDepositPercentage is < 0 or > 100)
            return Result<QuoteDto>.Fail("RequiredDepositPercentage must be between 0 and 100.");

        var quote = await _quotes.GetByIdAsync(id, ct);
        if (quote is null) return NotFound();
        if (quote.State != QuoteState.Issued) return Result<QuoteDto>.Fail("Only issued quotes can be accepted.", "QUOTE_NOT_ISSUED");
        var now = Now();
        if (quote.IsPastValidity(DateOnly.FromDateTime(now)))
            return Result<QuoteDto>.Fail("The quote has expired and can no longer be accepted.", "QUOTE_EXPIRED");

        return await ApplyAsync(quote, () => quote.Accept(request.RequiredDepositPercentage, now), ct);
    }

    public async Task<Result<QuoteDto>> PayDepositAsync(Guid id, PayQuoteDepositRequest request, CancellationToken ct = default)
    {
        var amount = Money.Round(request.Amount);
        if (amount <= 0) return Result<QuoteDto>.Fail("Amount must be at least one cent.");

        var quote = await _quotes.GetByIdAsync(id, ct);
        if (quote is null) return NotFound();
        if (quote.State != QuoteState.Accepted) return Result<QuoteDto>.Fail("Only accepted quotes can receive deposits.", "QUOTE_NOT_ACCEPTED");
        if (quote.DepositPaidAmount + amount > quote.Total)
            return Result<QuoteDto>.Fail("The deposits cannot add up to more than the quote total.", "QUOTE_DEPOSIT_TOO_HIGH");

        return await ApplyAsync(quote, () => quote.PayDeposit(amount), ct);
    }

    public async Task<Result<QuoteDto>> RejectAsync(Guid id, string reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) return Result<QuoteDto>.Fail("Reason is required.");
        if (reason.Trim().Length > MaxReasonLength) return Result<QuoteDto>.Fail($"Reason can be at most {MaxReasonLength} characters.");

        var quote = await _quotes.GetByIdAsync(id, ct);
        if (quote is null) return NotFound();
        if (quote.State != QuoteState.Issued) return Result<QuoteDto>.Fail("Only issued quotes can be rejected.", "QUOTE_NOT_ISSUED");

        var now = Now();
        return await ApplyAsync(quote, () => quote.Reject(reason, now), ct);
    }

    /// <summary>Withdraws a quote that is not decided yet: it can no longer be accepted.</summary>
    public async Task<Result<QuoteDto>> ExpireAsync(Guid id, CancellationToken ct = default)
    {
        var quote = await _quotes.GetByIdAsync(id, ct);
        if (quote is null) return NotFound();
        if (quote.State is QuoteState.Accepted or QuoteState.Rejected)
            return Result<QuoteDto>.Fail("An accepted or rejected quote cannot expire.", "QUOTE_ALREADY_DECIDED");

        var now = Now();
        return await ApplyAsync(quote, () => quote.Expire(now), ct);
    }

    public async Task<Result<WorkOrderDto>> ConvertAcceptedToWorkOrderAsync(Guid id, CancellationToken ct = default)
    {
        var quote = await _quotes.GetByIdAsync(id, ct);
        if (quote is null) return Result<WorkOrderDto>.Fail("Quote not found.", "QUOTE_NOT_FOUND");
        if (quote.State != QuoteState.Accepted)
            return Result<WorkOrderDto>.Fail("Only accepted quotes can create work orders.", "QUOTE_NOT_ACCEPTED");

        try
        {
            var workOrder = await _quotes.ConvertAcceptedToWorkOrderAsync(id, ct);
            // ConvertAcceptedToWorkOrderAsync saves internally, so this resolves in its own
            // immediate follow-up transaction, not piggybacked - see roadmap Phase 3 notes.
            await _commandJournal.ResolveNowAsync(_operationContext.OperationId, success: true, errorCode: null, ct);
            return Result<WorkOrderDto>.Ok(WorkOrderDto.MapFrom(workOrder));
        }
        catch (InvalidOperationException ex)
        {
            await _commandJournal.ResolveNowAsync(_operationContext.OperationId, success: false, "DOMAIN_VALIDATION_FAILED", ct);
            return Result<WorkOrderDto>.Fail(ex.Message);
        }
    }

    // ---- helpers -----------------------------------------------------------------------------------------

    /// <summary>
    /// Runs one change on the quote and saves it. The checks are the quote's own, and they run before anything is assigned, so a
    /// refused change leaves the quote as it was even though the journal is saved.
    /// </summary>
    private async Task<Result<QuoteDto>> ApplyAsync(Quote quote, Action change, CancellationToken ct)
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
        return Result<QuoteDto>.Ok(await MapAsync(quote, customer: null, ct));
    }

    private async Task<Result<QuoteDto>> RefusedByDomainAsync(Exception exception, CancellationToken ct)
    {
        await _commandJournal.ResolveNowAsync(_operationContext.OperationId, success: false, "DOMAIN_VALIDATION_FAILED", ct);
        return Result<QuoteDto>.Fail(exception.Message);
    }

    /// <summary>The address and the device of a quote must be the customer's own. Null when they are fine.</summary>
    private async Task<Result<QuoteDto>?> CheckPlaceAsync(Guid customerId, Guid? siteId, Guid? assetId, CancellationToken ct)
    {
        if (siteId is null)
            return assetId is null ? null : Result<QuoteDto>.Fail("A device belongs to an address: choose the address too.");

        var site = await _sites.GetSiteAsync(customerId, siteId.Value, ct);
        if (site is null) return Result<QuoteDto>.Fail("Address not found.", "SITE_NOT_FOUND");
        if (assetId is not null && await _sites.GetAssetAsync(site.Id, assetId.Value, ct) is null)
            return Result<QuoteDto>.Fail("Device not found.", "ASSET_NOT_FOUND");
        return null;
    }

    private DateTime Now() => _clock.GetUtcNow().UtcDateTime;

    private bool IsPast(DateOnly? validUntil) => validUntil is { } until && until < DateOnly.FromDateTime(Now());

    private static Result<QuoteDto> ValidityPast()
        => Result<QuoteDto>.Fail("The validity date cannot be in the past.", "QUOTE_VALIDITY_PAST");

    private static Result<QuoteDto> NotFound() => Result<QuoteDto>.Fail("Quote not found.", "QUOTE_NOT_FOUND");

    private static Result<QuoteDto> NotDraft() => Result<QuoteDto>.Fail("Only a draft quote can be changed.", "QUOTE_NOT_DRAFT");

    private static string? ValidateDetails(string? title, string? notes)
    {
        if (string.IsNullOrWhiteSpace(title)) return "Title is required.";
        if (title.Trim().Length > MaxTitleLength) return $"Title can be at most {MaxTitleLength} characters.";
        if (notes is not null && notes.Trim().Length > MaxNotesLength) return $"Notes can be at most {MaxNotesLength} characters.";
        return null;
    }

    /// <summary>The lines of a request as drafts, or the first thing wrong with them.</summary>
    private static string? ReadLines(IReadOnlyList<QuoteLineRequest>? requests, out List<QuoteItemDraft> lines)
    {
        lines = [];
        if (requests is null) return null;
        if (requests.Count > Quote.MaxItems) return $"A quote can have at most {Quote.MaxItems} lines.";

        for (var index = 0; index < requests.Count; index++)
        {
            if (requests[index] is null) return $"Line {index + 1} is empty.";
            var invalid = ReadLine(requests[index], out var line);
            if (invalid is not null) return $"Line {index + 1}: {invalid}";
            lines.Add(line!);
        }

        return null;
    }

    private static string? ReadLine(QuoteLineRequest request, out QuoteItemDraft? line)
    {
        line = null;
        if (string.IsNullOrWhiteSpace(request.Description)) return "Description is required.";
        if (request.Description.Trim().Length > MaxDescriptionLength) return $"Description can be at most {MaxDescriptionLength} characters.";
        if (request.Quantity <= 0 || request.Quantity > QuoteItem.MaxQuantity || Math.Round(request.Quantity, 2) != request.Quantity)
            return $"Quantity must be greater than zero and at most {QuoteItem.MaxQuantity:0}, with at most two decimals.";
        if (request.UnitPrice < 0 || request.UnitPrice > QuoteItem.MaxUnitPrice) return $"UnitPrice must be between 0 and {QuoteItem.MaxUnitPrice:0}.";
        var vatRate = request.VatRate ?? QuoteItem.DefaultVatRate;
        if (vatRate is < 0 or > 100) return "VatRate must be between 0 and 100.";
        if (request.Unit is not null && request.Unit.Trim().Length > MaxUnitLength) return $"Unit can be at most {MaxUnitLength} characters.";

        var kind = QuoteItemKind.Service;
        if (!string.IsNullOrWhiteSpace(request.Kind) && (!Enum.TryParse(request.Kind, ignoreCase: true, out kind) || !Enum.IsDefined(kind)))
            return "Unknown line kind.";

        line = new QuoteItemDraft(kind, request.Description, request.Quantity, request.UnitPrice, request.Unit, vatRate);
        return null;
    }

    private static QuoteSummaryDto MapSummary(Quote quote, IReadOnlyDictionary<Guid, string> names) => new(
        quote.Id, quote.CustomerId, names.GetValueOrDefault(quote.CustomerId, string.Empty), quote.Number, quote.Title, quote.State.ToString(),
        quote.Total, quote.ValidUntil, quote.CreatedAt, quote.RequiredDepositAmount, quote.DepositPaidAmount);

    /// <summary>The whole quote, with the customer, the address, the device and the work order it points at.</summary>
    private async Task<QuoteDto> MapAsync(Quote quote, Customer? customer, CancellationToken ct)
    {
        customer ??= await _customers.GetByIdAsync(quote.CustomerId, ct);

        CustomerSite? site = null;
        CustomerAsset? asset = null;
        if (quote.SiteId is { } siteId)
        {
            site = await _sites.GetSiteAsync(quote.CustomerId, siteId, ct);
            if (site is not null && quote.AssetId is { } assetId) asset = await _sites.GetAssetAsync(site.Id, assetId, ct);
        }

        var workOrder = quote.State == QuoteState.Accepted ? await _quotes.GetWorkOrderAsync(quote.Id, ct) : null;
        var lines = quote.Items.OrderBy(item => item.LineNumber).Select(item => new QuoteLineDto(
            item.Id, item.LineNumber, item.Kind.ToString(), item.Description, item.Unit, item.Quantity, item.UnitPrice, item.VatRate,
            item.LineTotal, item.VatAmount)).ToList();

        return new QuoteDto(
            quote.Id, quote.CustomerId, customer?.FullName ?? string.Empty, customer?.Phone ?? string.Empty, customer?.Email ?? string.Empty,
            customer?.TaxNumber ?? string.Empty, quote.Number, quote.Title, quote.Notes, quote.State.ToString(), quote.Total, quote.VatTotal,
            quote.CreatedAt, quote.IssuedAt, quote.ValidUntil, quote.DecidedAt, quote.RejectionReason, quote.RequiredDepositPercentage,
            quote.RequiredDepositAmount, quote.DepositPaidAmount, quote.SiteId, site?.Name, site?.Address, quote.AssetId, asset?.Name,
            quote.IsChangeOrder, quote.ParentWorkOrderId, workOrder?.Id, workOrder?.Number, lines);
    }
}
