using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Customers;
using Voltflow.Infrastructure.Persistence;
using Voltflow.Worker;
using static Voltflow.Tests.TestApi;

namespace Voltflow.Tests;

// What the in-memory database cannot show: that the migrations build the schema from zero, and that every query the
// screens rely on translates to SQL. Two bugs of the quick sale module hid exactly there until it ran on PostgreSQL.
[Xunit.Collection("PostgresIntegration")]
[Trait("VUT", "09901")]
public sealed class PostgresIntegrationTests : Xunit.IClassFixture<PostgresApiFixture>
{
    private readonly PostgresApiFixture _factory;

    public PostgresIntegrationTests(PostgresApiFixture factory) => _factory = factory;

    [PostgresFact]
    public async Task TheMigrations_BuildTheSchemaFromZero_AndTheModelHasNothingPending()
    {
        await using var scope = _factory.Services.CreateAsyncScope(); // starting the API has migrated the empty database
        var db = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();

        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.False(db.Database.HasPendingModelChanges(), "The model differs from the last migration: a migration is missing.");
        _ = await db.QuickSales.AnyAsync(); // throws when the table of a recent migration is missing
    }

    [PostgresFact]
    public async Task QuickSale_RunsThroughARealDatabase()
    {
        using var manager = await _factory.ActorAsync("Manager");
        using var cashier = await _factory.ActorAsync("Technician");

        var plug = await CreateProductAsync(manager, "PRIZ-16A", "Priz 16A topraklı", "8690000000017", 120m, 20m, stock: 50m);
        var cable = await CreateProductAsync(manager, "KABLO-3X15", "Kablo 3x1.5 NYM", "8690000000024", 45.5m, 10m, stock: 50m);
        var switchProduct = await CreateProductAsync(manager, "ANAHTAR-1", "Tekli anahtar", "8690000000031", 79.9m, 20m, stock: 3m);

        // the price list: search, and lookup by barcode or by code in any case
        using (var search = await manager.Client.GetAsync("/api/products?search=kablo"))
        {
            Assert.Equal("1", search.Headers.GetValues("X-Total-Count").Single());
            Assert.Equal(cable.Id, Assert.Single(await ReadAsync<List<ProductDto>>(search)).Id);
        }

        Assert.Equal(plug.Id, (await ReadAsync<ProductDto>(await cashier.Client.GetAsync("/api/products/lookup?term=8690000000017"))).Id);
        Assert.Equal(plug.Id, (await ReadAsync<ProductDto>(await cashier.Client.GetAsync("/api/products/lookup?term=priz-16a"))).Id);
        using var sameCode = await PostAsync(manager.Client, "/api/products", new CreateProductRequest("priz-16a", "Again", null, null, 1m, 20m, false));
        await AssertRejectedAsync(sameCode, "PRODUCT_CODE_EXISTS");

        // a sale needs a shift
        using var noShift = await PostAsync(cashier.Client, "/api/quick-sales", SaleOf(plug.Id, 1m, cash: 120m));
        await AssertRejectedAsync(noShift, "SHIFT_REQUIRED");
        var shift = await ReadAsync<CashShiftReportDto>(await PostAsync(cashier.Client, "/api/cash-shifts/open", new OpenShiftRequest(100m)));

        // a plain cash sale, then one split over card and cash with discounts and a free line
        var first = await ReadAsync<QuickSaleDto>(await PostAsync(cashier.Client, "/api/quick-sales", SaleOf(plug.Id, 2m, cash: 300m)));
        Assert.Equal((240m, 40m, 60m), (first.GrandTotal, first.VatTotal, first.ChangeGiven));

        var second = await ReadAsync<QuickSaleDto>(await PostAsync(cashier.Client, "/api/quick-sales", new CreateQuickSaleRequest(
            null,
            [
                new QuickSaleLineRequest(cable.Id, null, 4m, null, null, 2.5m),
                new QuickSaleLineRequest(null, "Kapı çıkış ücreti", 1m, 100m, 20m, 0m)
            ],
            10m,
            [new QuickSalePaymentRequest("Card", 100m, "slip-9"), new QuickSalePaymentRequest("Cash", 200m, null)],
            "kapı satışı")));
        Assert.Equal((269.5m, 30.5m), (second.GrandTotal, second.ChangeGiven));
        Assert.Equal(46m, await StockAsync(manager, "KABLO-3X15"));

        // sales that are refused leave nothing behind, not even a receipt number
        using var tooMany = await PostAsync(cashier.Client, "/api/quick-sales", SaleOf(switchProduct.Id, 4m, cash: 400m));
        await AssertRejectedAsync(tooMany, "INSUFFICIENT_STOCK");
        using var underpaid = await PostAsync(cashier.Client, "/api/quick-sales", SaleOf(switchProduct.Id, 1m, cash: 10m));
        await AssertRejectedAsync(underpaid, "SALE_REJECTED");
        Assert.Equal(3m, await StockAsync(manager, "ANAHTAR-1"));
        var third = await ReadAsync<QuickSaleDto>(await PostAsync(cashier.Client, "/api/quick-sales", new CreateQuickSaleRequest(
            null, [new QuickSaleLineRequest(switchProduct.Id, null, 1m, null, null, 0m)], 0m, [new QuickSalePaymentRequest("BankTransfer", 79.9m, "EFT-1")], null)));
        Assert.Equal(["HS-000001", "HS-000002", "HS-000003"], [first.SaleNumber, second.SaleNumber, third.SaleNumber]);

        // looking sales up: by number, by period, by status, in pages
        var byNumber = await ReadAsync<List<QuickSaleSummaryDto>>(await manager.Client.GetAsync("/api/quick-sales?search=hs-000002"));
        Assert.Equal(["Cash", "Card"], Assert.Single(byNumber).PaymentMethods);
        var from = Uri.EscapeDataString(DateTime.UtcNow.AddHours(-1).ToString("o"));
        var to = Uri.EscapeDataString(DateTime.UtcNow.AddHours(1).ToString("o"));
        Assert.Equal(3, (await ReadAsync<List<QuickSaleSummaryDto>>(await manager.Client.GetAsync($"/api/quick-sales?from={from}&to={to}"))).Count);
        Assert.Empty(await ReadAsync<List<QuickSaleSummaryDto>>(await manager.Client.GetAsync($"/api/quick-sales?to={from}")));
        Assert.Empty(await ReadAsync<List<QuickSaleSummaryDto>>(await manager.Client.GetAsync("/api/quick-sales?status=Voided")));
        using (var page = await manager.Client.GetAsync("/api/quick-sales?limit=2"))
        {
            Assert.Equal("3", page.Headers.GetValues("X-Total-Count").Single());
            Assert.Equal(2, (await ReadAsync<List<QuickSaleSummaryDto>>(page)).Count);
        }

        // a return of one plug, taken by the manager on the manager's own shift, then the second sale is voided
        var managerShift = await ReadAsync<CashShiftReportDto>(await PostAsync(manager.Client, "/api/cash-shifts/open", new OpenShiftRequest(200m)));
        var afterReturn = await ReadAsync<QuickSaleDto>(await PostAsync(manager.Client, $"/api/quick-sales/{first.Id}/returns",
            new ReturnQuickSaleRequest("faulty", "Cash", [new ReturnItemRequest(first.Lines.Single().Id, 1m)])));
        Assert.Equal(("IA-000001", 120m), (afterReturn.Returns.Single().ReturnNumber, afterReturn.Returns.Single().RefundTotal));
        Assert.Equal(49m, await StockAsync(manager, "PRIZ-16A"));
        var voided = await ReadAsync<QuickSaleDto>(await PostAsync(manager.Client, $"/api/quick-sales/{second.Id}/void", new VoidQuickSaleRequest("mistake")));
        Assert.Equal("Voided", voided.Status);
        Assert.Equal(50m, await StockAsync(manager, "KABLO-3X15"));

        // what the cashier's shift adds up to (the voided sale is out of it, the refund is not on it) ...
        var report = await ReadAsync<CashShiftReportDto>(await manager.Client.GetAsync($"/api/cash-shifts/{shift.Shift.Id}/report"));
        Assert.Equal((2, 1, 0), (report.SaleCount, report.VoidedCount, report.ReturnCount));
        Assert.Equal((319.9m, 240m, 0m, 79.9m, 0m), (report.SalesTotal, report.CashSales, report.CardSales, report.TransferSales, report.CashRefunds));
        Assert.Equal((319.9m, 340m), (report.NetSales, report.ExpectedCash));
        var bucket = Assert.Single(report.VatBreakdown);
        Assert.Equal((20m, 319.9m, 53.32m), (bucket.Rate, bucket.Gross, bucket.Vat));

        // ... and the manager's shift carries the refund
        var managerReport = await ReadAsync<CashShiftReportDto>(await manager.Client.GetAsync($"/api/cash-shifts/{managerShift.Shift.Id}/report"));
        Assert.Equal((1, 120m, 80m), (managerReport.ReturnCount, managerReport.CashRefunds, managerReport.ExpectedCash));

        // closing against the counted cash
        var closed = await ReadAsync<CashShiftReportDto>(await PostAsync(cashier.Client, $"/api/cash-shifts/{shift.Shift.Id}/close", new CloseShiftRequest(335m, "five short")));
        Assert.Equal(("Closed", -5m), (closed.Shift.Status, closed.Shift.CashDifference));
    }

    [PostgresFact]
    public async Task Customers_RunThroughARealDatabase()
    {
        using var manager = await _factory.ActorAsync("Manager");
        var tag = Guid.NewGuid().ToString("N")[..10];

        // the list narrows down by name, phone, email and tax number, in any case, and pages
        var door = await ReadAsync<CustomerDto>(await PostAsync(manager.Client, "/api/customers", new CreateCustomerRequest($"Ali {tag}", null, $"0532{tag[..6]}", null)));
        var company = await ReadAsync<CustomerDto>(await PostAsync(manager.Client, "/api/customers", new CreateCustomerRequest($"Firma {tag}", $"info-{tag}@firma.example", "02120000000", $"TAX{tag}")));
        Assert.Equal(door.Id, Assert.Single(await SearchAsync(manager, $"ALI {tag}")).Id);
        Assert.Equal(door.Id, Assert.Single(await SearchAsync(manager, $"0532{tag[..6]}")).Id);
        Assert.Equal(company.Id, Assert.Single(await SearchAsync(manager, $"INFO-{tag}")).Id);
        Assert.Equal(company.Id, Assert.Single(await SearchAsync(manager, $"tax{tag}")).Id);
        using (var page = await manager.Client.GetAsync($"/api/customers?search={tag}&limit=1"))
        {
            Assert.Equal("2", page.Headers.GetValues("X-Total-Count").Single());
            Assert.Single(await ReadAsync<List<CustomerDto>>(page));
        }

        Assert.Equal(2, (await ReadAsync<List<CustomerDto>>(await manager.Client.GetAsync($"/api/customers?search={tag}&type=Lead&active=true"))).Count);
        Assert.Empty(await ReadAsync<List<CustomerDto>>(await manager.Client.GetAsync($"/api/customers?search={tag}&active=false")));

        // any number of customers without a tax number or an email, but never two with the same tax number: the database itself says so
        await ReadAsync<CustomerDto>(await PostAsync(manager.Client, "/api/customers", new CreateCustomerRequest($"Blank one {tag}", null, "555", null)));
        await ReadAsync<CustomerDto>(await PostAsync(manager.Client, "/api/customers", new CreateCustomerRequest($"Blank two {tag}", null, "555", null)));
        using var sameTax = await PostAsync(manager.Client, "/api/customers", new CreateCustomerRequest($"Twin {tag}", null, "555", $"TAX{tag}"));
        await AssertRejectedAsync(sameTax, "CUSTOMER_TAXNUMBER_EXISTS");
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
            db.Customers.Add(new Customer($"Race one {tag}", null, "555", $"RACE{tag}"));
            db.Customers.Add(new Customer($"Race two {tag}", null, "555", $"RACE{tag}"));
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }

        // addresses and devices: blank serial numbers do not clash, a real one is unique
        var site = await ReadAsync<CustomerSiteDto>(await PostAsync(manager.Client, $"/api/customers/{door.Id}/sites", new SaveSiteRequest("Home", "1 Main Street")));
        var boiler = await ReadAsync<CustomerAssetDto>(await PostAsync(manager.Client, $"/api/customers/{door.Id}/sites/{site.Id}/assets", new SaveAssetRequest("Boiler", $"SN-{tag}", new DateOnly(2024, 5, 17))));
        await ReadAsync<CustomerAssetDto>(await PostAsync(manager.Client, $"/api/customers/{door.Id}/sites/{site.Id}/assets", new SaveAssetRequest("Thermostat", null, null)));
        await ReadAsync<CustomerAssetDto>(await PostAsync(manager.Client, $"/api/customers/{door.Id}/sites/{site.Id}/assets", new SaveAssetRequest("Radiator", null, null)));
        using var sameSerial = await PostAsync(manager.Client, $"/api/customers/{door.Id}/sites/{site.Id}/assets", new SaveAssetRequest("Copy", $"SN-{tag}", null));
        await AssertRejectedAsync(sameSerial, "ASSET_SERIAL_EXISTS");
        var sites = await ReadAsync<List<CustomerSiteDto>>(await manager.Client.GetAsync($"/api/customers/{door.Id}/sites"));
        Assert.Equal(["Boiler", "Radiator", "Thermostat"], Assert.Single(sites).Assets.Select(asset => asset.Name));
        Assert.Equal(new DateOnly(2024, 5, 17), sites[0].Assets.Single(asset => asset.Id == boiler.Id).InstallationDate);
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
            db.CustomerAssets.Add(new CustomerAsset(site.Id, "Race boiler", $"SN-{tag}", null));
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }

        // and the sales of a customer
        using var cashier = await _factory.ActorAsync("Technician");
        var product = await CreateProductAsync(manager, $"C-{tag}", "Thing", $"869{tag}", 10m, 0m, stock: 5m);
        await ReadAsync<CashShiftReportDto>(await PostAsync(cashier.Client, "/api/cash-shifts/open", new OpenShiftRequest(0m)));
        var sale = await ReadAsync<QuickSaleDto>(await PostAsync(cashier.Client, "/api/quick-sales", new CreateQuickSaleRequest(
            door.Id, [new QuickSaleLineRequest(product.Id, null, 1m, null, null, 0m)], 0m, [new QuickSalePaymentRequest("Cash", 10m, null)], null)));
        Assert.Equal(sale.Id, Assert.Single(await ReadAsync<List<QuickSaleSummaryDto>>(await manager.Client.GetAsync($"/api/quick-sales?customerId={door.Id}"))).Id);
    }

    [PostgresFact]
    public async Task Quotes_RunThroughARealDatabase()
    {
        using var manager = await _factory.ActorAsync("Manager");
        var tag = Guid.NewGuid().ToString("N")[..10];
        var customer = await ReadAsync<CustomerDto>(await PostAsync(manager.Client, "/api/customers", new CreateCustomerRequest($"Quote customer {tag}", null, "5550001")));
        var site = await ReadAsync<CustomerSiteDto>(await PostAsync(manager.Client, $"/api/customers/{customer.Id}/sites", new SaveSiteRequest("Villa", "Sahil Cd. 4")));
        var inverter = await ReadAsync<CustomerAssetDto>(await PostAsync(manager.Client, $"/api/customers/{customer.Id}/sites/{site.Id}/assets", new SaveAssetRequest("Inverter", $"SN-{tag}", null)));

        // the numbers come from one running counter that is saved with the quote, so a refused quote leaves no gap
        var quote = await ReadAsync<QuoteDto>(await PostAsync(manager.Client, "/api/quotes", new CreateQuoteRequest(
            customer.Id, $"Solar {tag}", "call first", SiteId: site.Id, AssetId: inverter.Id, Items:
            [
                new QuoteLineRequest("Fitting", 2m, 120m, "saat", 20m, "Labor"),
                new QuoteLineRequest("Cable", 1m, 110m, "metre", 10m, "Material"),
                new QuoteLineRequest("Advice", 1m, 50m, null, 0m)
            ])));
        Assert.Equal(("TK-000001", "Draft", 400m, 50m), (quote.Number, quote.State, quote.Total, quote.VatTotal));
        using var refused = await PostAsync(manager.Client, "/api/quotes", new CreateQuoteRequest(customer.Id, "Refused", Items: [new QuoteLineRequest("Zero", 0m, 1m)]));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, refused.StatusCode);
        var second = await ReadAsync<QuoteDto>(await PostAsync(manager.Client, "/api/quotes", new CreateQuoteRequest(customer.Id, $"Wiring {tag}", Items: [new QuoteLineRequest("Cable", 10m, 12.5m)])));
        Assert.Equal("TK-000002", second.Number);

        // replacing the lines of a draft replaces the rows: the old ones are deleted, the new ones inserted
        var changed = await ReadAsync<QuoteDto>(await PutAsync(manager.Client, $"/api/quotes/{quote.Id}", new UpdateQuoteRequest(
            $"Solar {tag}", null, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(20), site.Id, inverter.Id,
            [new QuoteLineRequest("Panels", 10m, 200m, "adet", 10m, "Material"), new QuoteLineRequest("Fitting", 8m, 75m, "saat", 20m, "Labor")])));
        Assert.Equal(2600m, changed.Total);
        Assert.Equal(["Panels", "Fitting"], changed.Items.Select(line => line.Description));
        var added = await ReadAsync<QuoteDto>(await PostAsync(manager.Client, $"/api/quotes/{quote.Id}/items", new QuoteLineRequest("Scaffolding", 1m, 500m, null, 20m)));
        Assert.Equal((3100m, 3), (added.Total, added.Items.Count));
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
            var stored = await db.Quotes.AsNoTracking().Include(candidate => candidate.Items).SingleAsync(candidate => candidate.Id == quote.Id);
            Assert.Equal(["Panels", "Fitting", "Scaffolding"], stored.Items.OrderBy(line => line.LineNumber).Select(line => line.Description));
            Assert.Equal(3100m, stored.Total);
        }

        // the list: by number, title and customer name in any case, by state, in pages, without loading the lines
        Assert.Equal(quote.Id, Assert.Single(await ReadAsync<List<QuoteSummaryDto>>(await manager.Client.GetAsync($"/api/quotes?search=SOLAR {tag}"))).Id);
        Assert.Equal(2, (await ReadAsync<List<QuoteSummaryDto>>(await manager.Client.GetAsync($"/api/quotes?search=QUOTE CUSTOMER {tag}"))).Count);
        Assert.Equal(second.Id, Assert.Single(await ReadAsync<List<QuoteSummaryDto>>(await manager.Client.GetAsync("/api/quotes?search=tk-000002"))).Id);
        Assert.Equal(2, (await ReadAsync<List<QuoteSummaryDto>>(await manager.Client.GetAsync($"/api/quotes?customerId={customer.Id}&state=Draft"))).Count);
        using (var page = await manager.Client.GetAsync($"/api/quotes?customerId={customer.Id}&limit=1"))
        {
            Assert.Equal("2", page.Headers.GetValues("X-Total-Count").Single());
            Assert.Equal(second.Id, Assert.Single(await ReadAsync<List<QuoteSummaryDto>>(page)).Id); // newest first
        }

        // issue, accept, take deposits, make the work order, revise: the whole way
        var issued = await ReadAsync<QuoteDto>(await PostAsync(manager.Client, $"/api/quotes/{quote.Id}/issue"));
        Assert.Equal("Issued", issued.State);
        await ReadAsync<QuoteDto>(await PostAsync(manager.Client, $"/api/quotes/{quote.Id}/accept", new AcceptQuoteRequest(30m)));
        var deposit = await ReadAsync<QuoteDto>(await PostAsync(manager.Client, $"/api/quotes/{quote.Id}/pay-deposit", new PayQuoteDepositRequest(930m)));
        Assert.Equal((930m, 930m), (deposit.RequiredDepositAmount, deposit.DepositPaidAmount));
        var workOrder = await ReadAsync<WorkOrderDto>(await PostAsync(manager.Client, $"/api/quotes/{quote.Id}/work-order"));
        Assert.Equal(3100m, workOrder.Total);
        Assert.Equal(workOrder.Number, (await ReadAsync<QuoteDto>(await manager.Client.GetAsync($"/api/quotes/{quote.Id}"))).WorkOrderNumber);
        var copy = await ReadAsync<QuoteDto>(await PostAsync(manager.Client, $"/api/quotes/{quote.Id}/copy"));
        Assert.Equal(("TK-000003", "Draft", 3100m, 3), (copy.Number, copy.State, copy.Total, copy.Items.Count));
        using var deleted = await DeleteAsync(manager.Client, $"/api/quotes/{copy.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        // the daily sweep: an issued quote whose last day has passed expires, and nothing else does
        var lapsing = await ReadAsync<QuoteDto>(await PostAsync(manager.Client, "/api/quotes", new CreateQuoteRequest(
            customer.Id, $"Lapsing {tag}", ValidUntil: DateOnly.FromDateTime(DateTime.UtcNow), Items: [new QuoteLineRequest("Line", 1m, 100m)])));
        await ReadAsync<QuoteDto>(await PostAsync(manager.Client, $"/api/quotes/{lapsing.Id}/issue"));
        var open = await ReadAsync<QuoteDto>(await PostAsync(manager.Client, $"/api/quotes/{second.Id}/issue"));
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
            var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"Quotes\" SET \"ValidUntil\" = {yesterday} WHERE \"Id\" = {lapsing.Id}");
        }

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var processor = new QuoteExpiryProcessor(scope.ServiceProvider.GetRequiredService<IQuoteRepository>(), NullLogger<QuoteExpiryProcessor>.Instance);
            Assert.Equal(1, await processor.ExpireDueAsync(DateTime.UtcNow));
            Assert.Equal(0, await processor.ExpireDueAsync(DateTime.UtcNow));
        }

        Assert.Equal("Expired", (await ReadAsync<QuoteDto>(await manager.Client.GetAsync($"/api/quotes/{lapsing.Id}"))).State);
        Assert.Equal("Issued", (await ReadAsync<QuoteDto>(await manager.Client.GetAsync($"/api/quotes/{open.Id}"))).State);
        Assert.Equal("Accepted", (await ReadAsync<QuoteDto>(await manager.Client.GetAsync($"/api/quotes/{quote.Id}"))).State);
    }

    private static async Task<List<CustomerDto>> SearchAsync(Actor actor, string search)
        => await ReadAsync<List<CustomerDto>>(await actor.Client.GetAsync($"/api/customers?search={Uri.EscapeDataString(search)}"));

    private static CreateQuickSaleRequest SaleOf(Guid productId, decimal quantity, decimal cash)
        => new(null, [new QuickSaleLineRequest(productId, null, quantity, null, null, 0m)], 0m, [new QuickSalePaymentRequest("Cash", cash, null)], null);

    private static async Task<ProductDto> CreateProductAsync(Actor manager, string code, string name, string barcode, decimal price, decimal vat, decimal stock)
    {
        var product = await ReadAsync<ProductDto>(await PostAsync(manager.Client, "/api/products", new CreateProductRequest(code, name, barcode, "adet", price, vat, true)));
        await ReadAsync<StockDto>(await PostAsync(manager.Client, "/api/inventory/adjust", new AdjustStockRequest(code, stock)));
        return product;
    }

    private static async Task<decimal> StockAsync(Actor actor, string code)
        => (await ReadAsync<StockDto>(await actor.Client.GetAsync($"/api/inventory/{code}"))).QuantityOnHand;
}
