using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Customers;
using Voltflow.Shared;

namespace Voltflow.Application.Services;

/// <summary>The addresses of a customer and the devices installed at them. Everything is checked before anything changes, then committed once.</summary>
public sealed class CustomerSiteService : ICustomerSiteService
{
    private const int MaxSiteNameLength = 100;
    private const int MaxAddressLength = 500;
    private const int MaxAssetNameLength = 200;
    private const int MaxSerialNumberLength = 100;

    private readonly ICustomerRepository _customers;
    private readonly ICustomerSiteRepository _sites;
    private readonly ICommandJournal _commandJournal;
    private readonly IOperationContext _operationContext;
    private readonly IUnitOfWork _unitOfWork;

    public CustomerSiteService(
        ICustomerRepository customers,
        ICustomerSiteRepository sites,
        ICommandJournal commandJournal,
        IOperationContext operationContext,
        IUnitOfWork unitOfWork)
    {
        _customers = customers;
        _sites = sites;
        _commandJournal = commandJournal;
        _operationContext = operationContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyList<CustomerSiteDto>>> ListAsync(Guid customerId, CancellationToken ct = default)
    {
        if (await _customers.GetByIdAsync(customerId, ct) is null)
            return Result<IReadOnlyList<CustomerSiteDto>>.Fail("Customer not found.", "CUSTOMER_NOT_FOUND");

        var sites = await _sites.ListByCustomerAsync(customerId, ct);
        var assets = await _sites.ListAssetsAsync(sites.Select(site => site.Id).ToList(), ct);
        IReadOnlyList<CustomerSiteDto> result = sites.Select(site => MapSite(site, assets.Where(asset => asset.SiteId == site.Id))).ToList();
        return Result<IReadOnlyList<CustomerSiteDto>>.Ok(result);
    }

    public async Task<Result<CustomerSiteDto>> CreateSiteAsync(Guid customerId, SaveSiteRequest request, CancellationToken ct = default)
    {
        var invalid = ValidateSite(request);
        if (invalid is not null) return Result<CustomerSiteDto>.Fail(invalid);
        if (await _customers.GetByIdAsync(customerId, ct) is null) return Result<CustomerSiteDto>.Fail("Customer not found.", "CUSTOMER_NOT_FOUND");

        var site = new CustomerSite(customerId, request.Name.Trim(), request.Address.Trim());
        if (!request.IsActive) site.SetInactive();
        _sites.AddSite(site);
        _commandJournal.MarkResolved(_operationContext.OperationId, success: true, errorCode: null);
        await _unitOfWork.CommitAsync(ct);

        return Result<CustomerSiteDto>.Ok(MapSite(site, []));
    }

    public async Task<Result<CustomerSiteDto>> UpdateSiteAsync(Guid customerId, Guid siteId, SaveSiteRequest request, CancellationToken ct = default)
    {
        var invalid = ValidateSite(request);
        if (invalid is not null) return Result<CustomerSiteDto>.Fail(invalid);

        var site = await _sites.GetSiteAsync(customerId, siteId, ct);
        if (site is null) return Result<CustomerSiteDto>.Fail("Address not found.", "SITE_NOT_FOUND");

        site.Update(request.Name.Trim(), request.Address.Trim());
        if (request.IsActive != site.IsActive)
        {
            if (request.IsActive) site.SetActive();
            else site.SetInactive();
        }

        _commandJournal.MarkResolved(_operationContext.OperationId, success: true, errorCode: null);
        await _unitOfWork.CommitAsync(ct);

        var assets = await _sites.ListAssetsAsync([site.Id], ct);
        return Result<CustomerSiteDto>.Ok(MapSite(site, assets));
    }

    public async Task<Result<CustomerAssetDto>> CreateAssetAsync(Guid customerId, Guid siteId, SaveAssetRequest request, CancellationToken ct = default)
    {
        var invalid = ValidateAsset(request);
        if (invalid is not null) return Result<CustomerAssetDto>.Fail(invalid);

        var site = await _sites.GetSiteAsync(customerId, siteId, ct);
        if (site is null) return Result<CustomerAssetDto>.Fail("Address not found.", "SITE_NOT_FOUND");

        var serial = request.SerialNumber?.Trim() ?? string.Empty;
        if (serial.Length > 0 && await _sites.SerialNumberExistsAsync(serial, exceptAssetId: null, ct))
            return Result<CustomerAssetDto>.Fail("A device with this serial number already exists.", "ASSET_SERIAL_EXISTS");

        var asset = new CustomerAsset(site.Id, request.Name.Trim(), serial, request.InstallationDate);
        if (!request.IsActive) asset.SetInactive();
        _sites.AddAsset(asset);
        _commandJournal.MarkResolved(_operationContext.OperationId, success: true, errorCode: null);
        await _unitOfWork.CommitAsync(ct);

        return Result<CustomerAssetDto>.Ok(MapAsset(asset));
    }

    public async Task<Result<CustomerAssetDto>> UpdateAssetAsync(Guid customerId, Guid siteId, Guid assetId, SaveAssetRequest request, CancellationToken ct = default)
    {
        var invalid = ValidateAsset(request);
        if (invalid is not null) return Result<CustomerAssetDto>.Fail(invalid);

        var site = await _sites.GetSiteAsync(customerId, siteId, ct);
        if (site is null) return Result<CustomerAssetDto>.Fail("Address not found.", "SITE_NOT_FOUND");
        var asset = await _sites.GetAssetAsync(site.Id, assetId, ct);
        if (asset is null) return Result<CustomerAssetDto>.Fail("Device not found.", "ASSET_NOT_FOUND");

        var serial = request.SerialNumber?.Trim() ?? string.Empty;
        if (serial.Length > 0 && await _sites.SerialNumberExistsAsync(serial, exceptAssetId: asset.Id, ct))
            return Result<CustomerAssetDto>.Fail("A device with this serial number already exists.", "ASSET_SERIAL_EXISTS");

        asset.Update(request.Name.Trim(), serial, request.InstallationDate);
        if (request.IsActive != asset.IsActive)
        {
            if (request.IsActive) asset.SetActive();
            else asset.SetInactive();
        }

        _commandJournal.MarkResolved(_operationContext.OperationId, success: true, errorCode: null);
        await _unitOfWork.CommitAsync(ct);

        return Result<CustomerAssetDto>.Ok(MapAsset(asset));
    }

    private static string? ValidateSite(SaveSiteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return "Name is required.";
        if (request.Name.Trim().Length > MaxSiteNameLength) return $"Name can be at most {MaxSiteNameLength} characters.";
        if (string.IsNullOrWhiteSpace(request.Address)) return "Address is required.";
        if (request.Address.Trim().Length > MaxAddressLength) return $"Address can be at most {MaxAddressLength} characters.";
        return null;
    }

    private static string? ValidateAsset(SaveAssetRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return "Name is required.";
        if (request.Name.Trim().Length > MaxAssetNameLength) return $"Name can be at most {MaxAssetNameLength} characters.";
        if (request.SerialNumber is not null && request.SerialNumber.Trim().Length > MaxSerialNumberLength)
            return $"SerialNumber can be at most {MaxSerialNumberLength} characters.";
        return null;
    }

    private static CustomerSiteDto MapSite(CustomerSite site, IEnumerable<CustomerAsset> assets) =>
        new(site.Id, site.CustomerId, site.Name, site.Address, site.IsActive, assets.Select(MapAsset).ToList());

    private static CustomerAssetDto MapAsset(CustomerAsset asset) =>
        new(asset.Id, asset.SiteId, asset.Name, asset.SerialNumber, asset.InstallationDate, asset.IsActive);
}
