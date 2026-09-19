using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voltflow.Application.Dtos;
using Voltflow.Domain.Inventory;
using Voltflow.Infrastructure.Persistence;
using static Voltflow.Tests.TestApi;

namespace Voltflow.Tests;

// The counter end to end: price list, shifts, sales, void and return, through the real API with the real rules.
// Every test brings its own users, products and stock, so they do not depend on each other or on the order they run in.
[Xunit.Collection("ApiIntegration")]
[Trait("VUT", "09101")]
public sealed class QuickSaleIntegrationTests : Xunit.IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _factory;

    public QuickSaleIntegrationTests(ApiTestFixture factory) => _factory = factory;

    // ---- shifts ----------------------------------------------------------------------------------------

    [Fact]
    [Trait("VUT", "09201")]
    public async Task ASale_RunsFromOpeningTheShiftToClosingIt()
    {
        using var manager = await _factory.ActorAsync("Manager");
        using var cashier = await _factory.ActorAsync("Technician");
        var code = NewCode();
        var product = await CreateProductAsync(manager, code, price: 120m, vat: 20m, stock: 10m);

        var opened = await ReadAsync<CashShiftReportDto>(await PostAsync(cashier.Client, "/api/cash-shifts/open", new OpenShiftRequest(100m)));
        Assert.Equal("Open", opened.Shift.Status);
        Assert.Equal(100m, opened.ExpectedCash);

        var sale = await ReadAsync<QuickSaleDto>(await PostAsync(cashier.Client, "/api/quick-sales", SaleOf(product.Id, 2m, cash: 300m)));

        Assert.StartsWith("HS-", sale.SaleNumber, StringComparison.Ordinal);
        Assert.Equal("Completed", sale.Status);
        Assert.Equal(240m, sale.GrandTotal);
        Assert.Equal(40m, sale.VatTotal);
        Assert.Equal(300m, sale.CashTendered);
        Assert.Equal(60m, sale.ChangeGiven);
        var line = Assert.Single(sale.Lines);
        Assert.Equal(code, line.ProductCode);
        Assert.Equal(2m, line.Quantity);
        var payment = Assert.Single(sale.Payments);
        Assert.Equal("Cash", payment.Method);
        Assert.Equal(240m, payment.Amount);
        Assert.Equal(8m, await StockAsync(code));

        var report = await ReadAsync<CashShiftReportDto>(await cashier.Client.GetAsync($"/api/cash-shifts/{opened.Shift.Id}/report"));
        Assert.Equal(1, report.SaleCount);
        Assert.Equal(240m, report.SalesTotal);
        Assert.Equal(240m, report.CashSales);
        Assert.Equal(340m, report.ExpectedCash);
        var bucket = Assert.Single(report.VatBreakdown);
        Assert.Equal((20m, 240m, 40m), (bucket.Rate, bucket.Gross, bucket.Vat));

        var closed = await ReadAsync<CashShiftReportDto>(
            await PostAsync(cashier.Client, $"/api/cash-shifts/{opened.Shift.Id}/close", new CloseShiftRequest(335m, "five short")));
        Assert.Equal("Closed", closed.Shift.Status);
        Assert.Equal(340m, closed.Shift.ExpectedCash);
        Assert.Equal(-5m, closed.Shift.CashDifference);

        // a closed shift takes no more sales
        using var rejected = await PostAsync(cashier.Client, "/api/quick-sales", SaleOf(product.Id, 1m, cash: 120m));
        await AssertRejectedAsync(rejected, "SHIFT_REQUIRED");
    }

    [Fact]
    [Trait("VUT", "09202")]
    public async Task AShift_IsOpenedOncePerCashier_AndTheCurrentOneCanBeAskedFor()
    {
        using var cashier = await _factory.ActorAsync("Technician");

        using var none = await cashier.Client.GetAsync("/api/cash-shifts/current");
        Assert.Equal(HttpStatusCode.NoContent, none.StatusCode);

        var opened = await ReadAsync<CashShiftReportDto>(await PostAsync(cashier.Client, "/api/cash-shifts/open", new OpenShiftRequest(50m)));
        var current = await ReadAsync<CashShiftReportDto>(await cashier.Client.GetAsync("/api/cash-shifts/current"));
        Assert.Equal(opened.Shift.Id, current.Shift.Id);

        using var again = await PostAsync(cashier.Client, "/api/cash-shifts/open", new OpenShiftRequest(10m));
        await AssertRejectedAsync(again, "SHIFT_ALREADY_OPEN");

        using var negative = await PostAsync(cashier.Client, "/api/cash-shifts/open", new OpenShiftRequest(-1m));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, negative.StatusCode);
    }

    [Fact]
    [Trait("VUT", "09203")]
    public async Task AShift_CanBeClosedByItsCashierOrAManager_ButNotByAnotherCashier()
    {
        using var manager = await _factory.ActorAsync("Manager");
        using var cashier = await _factory.ActorAsync("Technician");
        using var colleague = await _factory.ActorAsync("Technician");
        var opened = await ReadAsync<CashShiftReportDto>(await PostAsync(cashier.Client, "/api/cash-shifts/open", new OpenShiftRequest(0m)));

        using var byColleague = await PostAsync(colleague.Client, $"/api/cash-shifts/{opened.Shift.Id}/close", new CloseShiftRequest(0m, null));
        await AssertRejectedAsync(byColleague, "SHIFT_NOT_FOUND");
        using var reportForColleague = await colleague.Client.GetAsync($"/api/cash-shifts/{opened.Shift.Id}/report");
        Assert.Equal(HttpStatusCode.NotFound, reportForColleague.StatusCode);

        var byManager = await ReadAsync<CashShiftReportDto>(await PostAsync(manager.Client, $"/api/cash-shifts/{opened.Shift.Id}/close", new CloseShiftRequest(0m, null)));
        Assert.Equal("Closed", byManager.Shift.Status);

        using var twice = await PostAsync(cashier.Client, $"/api/cash-shifts/{opened.Shift.Id}/close", new CloseShiftRequest(0m, null));
        await AssertRejectedAsync(twice, "SHIFT_CLOSED");
    }

    // ---- selling ---------------------------------------------------------------------------------------

    [Fact]
    [Trait("VUT", "09101")]
    public async Task ASale_NeedsAnOpenShift()
    {
        using var manager = await _factory.ActorAsync("Manager");
        using var cashier = await _factory.ActorAsync("Technician");
        var product = await CreateProductAsync(manager, NewCode(), stock: 5m);

        using var response = await PostAsync(cashier.Client, "/api/quick-sales", SaleOf(product.Id, 1m, cash: 120m));

        await AssertRejectedAsync(response, "SHIFT_REQUIRED");
    }

    [Fact]
    [Trait("VUT", "09102")]
    public async Task ARejectedSale_ChangesNothing_NotTheStockNorTheReceiptNumber()
    {
        using var manager = await _factory.ActorAsync("Manager");
        using var cashier = await _factory.ActorAsync("Technician");
        var code = NewCode();
        var product = await CreateProductAsync(manager, code, price: 100m, vat: 0m, stock: 1m);
        await PostAsync(cashier.Client, "/api/cash-shifts/open", new OpenShiftRequest(0m));
        var counterBefore = await CounterAsync("quick-sale");

        using var tooMany = await PostAsync(cashier.Client, "/api/quick-sales", SaleOf(product.Id, 2m, cash: 200m));
        await AssertRejectedAsync(tooMany, "INSUFFICIENT_STOCK");

        using var underpaid = await PostAsync(cashier.Client, "/api/quick-sales", SaleOf(product.Id, 1m, cash: 99m));
        await AssertRejectedAsync(underpaid, "SALE_REJECTED");

        using var overDiscounted = await PostAsync(cashier.Client, "/api/quick-sales", SaleOf(product.Id, 1m, cash: 100m, receiptDiscount: 101m));
        await AssertRejectedAsync(overDiscounted, "SALE_REJECTED");

        Assert.Equal(1m, await StockAsync(code));
        Assert.Equal(0, await SaleCountAsync(cashier.UserId));
        Assert.Equal(counterBefore, await CounterAsync("quick-sale"));
        Assert.Empty(await MovementsAsync(code));

        // and the next sale takes the very next number, so a rejected one leaves no gap
        var sale = await ReadAsync<QuickSaleDto>(await PostAsync(cashier.Client, "/api/quick-sales", SaleOf(product.Id, 1m, cash: 100m)));
        Assert.Equal($"HS-{counterBefore + 1:D6}", sale.SaleNumber);
        Assert.Equal(0m, await StockAsync(code));
    }

    [Fact]
    [Trait("VUT", "09103")]
    public async Task ASale_SplitsBetweenCardAndCash_AppliesDiscounts_AndAllowsFreeLines()
    {
        using var manager = await _factory.ActorAsync("Manager");
        using var cashier = await _factory.ActorAsync("Technician");
        var code = NewCode();
        var product = await CreateProductAsync(manager, code, price: 50m, vat: 10m, stock: 20m);
        await PostAsync(cashier.Client, "/api/cash-shifts/open", new OpenShiftRequest(0m));

        var request = new CreateQuickSaleRequest(
            null,
            [
                new QuickSaleLineRequest(product.Id, null, 4m, null, null, DiscountAmount: 20m),            // 200 - 20 = 180
                new QuickSaleLineRequest(null, "Door call-out", 1m, 100m, 20m, DiscountAmount: 0m)            // free line, no stock
            ],
            ReceiptDiscount: 30m,                                                                                // 280 - 30 = 250
            [
                new QuickSalePaymentRequest("Card", 100m, "slip-77"),
                new QuickSalePaymentRequest("Cash", 200m, null)
            ],
            "Sold at the door");

        var sale = await ReadAsync<QuickSaleDto>(await PostAsync(cashier.Client, "/api/quick-sales", request));

        Assert.Equal(300m, sale.Subtotal);
        Assert.Equal(20m, sale.LineDiscountTotal);
        Assert.Equal(30m, sale.ReceiptDiscount);
        Assert.Equal(250m, sale.GrandTotal);
        Assert.Equal(50m, sale.ChangeGiven);
        Assert.Equal(new[] { "Cash", "Card" }, sale.Payments.Select(payment => payment.Method));
        Assert.Equal(150m, sale.Payments.Single(payment => payment.Method == "Cash").Amount);
        Assert.Equal(100m, sale.Payments.Single(payment => payment.Method == "Card").Amount);
        Assert.Equal(new[] { "Door call-out" }, sale.Lines.Where(line => line.ProductId is null).Select(line => line.Description));
        Assert.Equal(16m, await StockAsync(code)); // only the listed product moved stock
        Assert.Equal(sale.Lines.Sum(line => line.LineTotal), sale.GrandTotal);
        Assert.Equal(sale.Lines.Sum(line => line.VatAmount), sale.VatTotal);
    }

    [Fact]
    [Trait("VUT", "09104")]
    public async Task ASale_CannotUseAnInactiveOrUnknownProduct_OrABadQuantity()
    {
        using var manager = await _factory.ActorAsync("Manager");
        using var cashier = await _factory.ActorAsync("Technician");
        var product = await CreateProductAsync(manager, NewCode(), stock: 5m);
        await PostAsync(cashier.Client, "/api/cash-shifts/open", new OpenShiftRequest(0m));

        using var unknown = await PostAsync(cashier.Client, "/api/quick-sales", SaleOf(Guid.NewGuid(), 1m, cash: 120m));
        await AssertRejectedAsync(unknown, "PRODUCT_NOT_FOUND");

        using var fractions = await PostAsync(cashier.Client, "/api/quick-sales", SaleOf(product.Id, 1.005m, cash: 500m));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, fractions.StatusCode);
        using var zero = await PostAsync(cashier.Client, "/api/quick-sales", SaleOf(product.Id, 0m, cash: 500m));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, zero.StatusCode);

        var update = new UpdateProductRequest(product.Name, product.Barcode, product.Unit, product.SalePrice, product.VatRate, product.TracksStock, IsActive: false);
        await ReadAsync<ProductDto>(await PutAsync(manager.Client, $"/api/products/{product.Id}", update));
        using var inactive = await PostAsync(cashier.Client, "/api/quick-sales", SaleOf(product.Id, 1m, cash: 120m));
        await AssertRejectedAsync(inactive, "PRODUCT_INACTIVE");
    }

    [Fact]
    [Trait("VUT", "09105")]
    public async Task ASale_CanNameACustomer_ButNotOneThatDoesNotExist()
    {
        using var manager = await _factory.ActorAsync("Manager");
        using var cashier = await _factory.ActorAsync("Technician");
        var product = await CreateProductAsync(manager, NewCode(), stock: 5m);
        await PostAsync(cashier.Client, "/api/cash-shifts/open", new OpenShiftRequest(0m));

        var customer = new Voltflow.Domain.Customers.Customer("Door Customer", "door@example.com", "5550000000");
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
            db.Customers.Add(customer);
            await db.SaveChangesAsync();
        }

        var customerId = customer.Id;

        var known = SaleOf(product.Id, 1m, cash: 120m) with { CustomerId = customerId };
        var sale = await ReadAsync<QuickSaleDto>(await PostAsync(cashier.Client, "/api/quick-sales", known));
        Assert.Equal(customerId, sale.CustomerId);
        Assert.Equal("Door Customer", sale.CustomerName);

        using var unknown = await PostAsync(cashier.Client, "/api/quick-sales", SaleOf(product.Id, 1m, cash: 120m) with { CustomerId = Guid.NewGuid() });
        await AssertRejectedAsync(unknown, "CUSTOMER_NOT_FOUND");
    }

    // ---- void ------------------------------------------------------------------------------------------

    [Fact]
    [Trait("VUT", "09301")]
    public async Task AVoidedSale_PutsTheStockBack_AndLeavesTheShiftFigures()
    {
        using var manager = await _factory.ActorAsync("Manager");
        using var cashier = await _factory.ActorAsync("Technician");
        var code = NewCode();
        var product = await CreateProductAsync(manager, code, price: 100m, vat: 20m, stock: 10m);
        var shift = await ReadAsync<CashShiftReportDto>(await PostAsync(cashier.Client, "/api/cash-shifts/open", new OpenShiftRequest(50m)));
        var sale = await ReadAsync<QuickSaleDto>(await PostAsync(cashier.Client, "/api/quick-sales", SaleOf(product.Id, 3m, cash: 300m)));
        Assert.Equal(7m, await StockAsync(code));

        using var byCashier = await PostAsync(cashier.Client, $"/api/quick-sales/{sale.Id}/void", new VoidQuickSaleRequest("mistake"));
        Assert.Equal(HttpStatusCode.Forbidden, byCashier.StatusCode);
        using var withoutReason = await PostAsync(manager.Client, $"/api/quick-sales/{sale.Id}/void", new VoidQuickSaleRequest(" "));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, withoutReason.StatusCode);
        Assert.Equal(7m, await StockAsync(code));

        var voided = await ReadAsync<QuickSaleDto>(await PostAsync(manager.Client, $"/api/quick-sales/{sale.Id}/void", new VoidQuickSaleRequest("customer changed their mind")));
        Assert.Equal("Voided", voided.Status);
        Assert.Equal("customer changed their mind", voided.VoidReason);
        Assert.Equal(10m, await StockAsync(code));

        var report = await ReadAsync<CashShiftReportDto>(await manager.Client.GetAsync($"/api/cash-shifts/{shift.Shift.Id}/report"));
        Assert.Equal(0, report.SaleCount);
        Assert.Equal(1, report.VoidedCount);
        Assert.Equal(0m, report.SalesTotal);
        Assert.Equal(50m, report.ExpectedCash);

        using var twice = await PostAsync(manager.Client, $"/api/quick-sales/{sale.Id}/void", new VoidQuickSaleRequest("again"));
        await AssertRejectedAsync(twice, "SALE_NOT_COMPLETED");
        Assert.Equal(10m, await StockAsync(code));
    }

    [Fact]
    [Trait("VUT", "09302")]
    public async Task ASale_CannotBeVoided_OnceItsShiftIsClosed_OrAfterAReturn()
    {
        using var manager = await _factory.ActorAsync("Manager");
        using var cashier = await _factory.ActorAsync("Technician");
        var code = NewCode();
        var product = await CreateProductAsync(manager, code, price: 100m, vat: 0m, stock: 10m);
        await PostAsync(manager.Client, "/api/cash-shifts/open", new OpenShiftRequest(0m));
        var shift = await ReadAsync<CashShiftReportDto>(await PostAsync(cashier.Client, "/api/cash-shifts/open", new OpenShiftRequest(0m)));
        var afterReturn = await ReadAsync<QuickSaleDto>(await PostAsync(cashier.Client, "/api/quick-sales", SaleOf(product.Id, 2m, cash: 200m)));
        var afterClose = await ReadAsync<QuickSaleDto>(await PostAsync(cashier.Client, "/api/quick-sales", SaleOf(product.Id, 1m, cash: 100m)));

        var returnRequest = new ReturnQuickSaleRequest("faulty", "Cash", [new ReturnItemRequest(afterReturn.Lines.Single().Id, 1m)]);
        await ReadAsync<QuickSaleDto>(await PostAsync(manager.Client, $"/api/quick-sales/{afterReturn.Id}/returns", returnRequest));
        using var hasReturns = await PostAsync(manager.Client, $"/api/quick-sales/{afterReturn.Id}/void", new VoidQuickSaleRequest("too late"));
        await AssertRejectedAsync(hasReturns, "SALE_HAS_RETURNS");

        await PostAsync(cashier.Client, $"/api/cash-shifts/{shift.Shift.Id}/close", new CloseShiftRequest(300m, null));
        using var closed = await PostAsync(manager.Client, $"/api/quick-sales/{afterClose.Id}/void", new VoidQuickSaleRequest("too late"));
        await AssertRejectedAsync(closed, "SHIFT_CLOSED");

        var stillThere = await ReadAsync<QuickSaleDto>(await manager.Client.GetAsync($"/api/quick-sales/{afterClose.Id}"));
        Assert.Equal("Completed", stillThere.Status);
        Assert.Equal(8m, await StockAsync(code)); // 10 - 2 - 1 + 1 back from the return
    }

    // ---- return ----------------------------------------------------------------------------------------

    [Fact]
    [Trait("VUT", "09401")]
    public async Task AReturn_RefundsTheLine_PutsStockBack_AndLeavesTheDrawerOfWhoTookItBack()
    {
        using var manager = await _factory.ActorAsync("Manager");
        using var cashier = await _factory.ActorAsync("Technician");
        var code = NewCode();
        var product = await CreateProductAsync(manager, code, price: 100m, vat: 20m, stock: 10m);
        var managerShift = await ReadAsync<CashShiftReportDto>(await PostAsync(manager.Client, "/api/cash-shifts/open", new OpenShiftRequest(500m)));
        await PostAsync(cashier.Client, "/api/cash-shifts/open", new OpenShiftRequest(0m));
        var sale = await ReadAsync<QuickSaleDto>(await PostAsync(cashier.Client, "/api/quick-sales", SaleOf(product.Id, 3m, cash: 300m, lineDiscount: 30m)));
        Assert.Equal(270m, sale.GrandTotal);
        var lineId = sale.Lines.Single().Id;

        var request = new ReturnQuickSaleRequest("faulty", "Cash", [new ReturnItemRequest(lineId, 1m)]);
        var returned = await ReadAsync<QuickSaleDto>(await PostAsync(manager.Client, $"/api/quick-sales/{sale.Id}/returns", request));

        var saleReturn = Assert.Single(returned.Returns);
        Assert.StartsWith("IA-", saleReturn.ReturnNumber, StringComparison.Ordinal);
        Assert.Equal(90m, saleReturn.RefundTotal);                    // a third of what was actually paid for the line
        Assert.Equal("Cash", saleReturn.RefundMethod);
        Assert.Equal(1m, returned.Lines.Single().ReturnedQuantity);
        Assert.Equal(8m, await StockAsync(code));                     // 10 - 3 + 1

        var managerReport = await ReadAsync<CashShiftReportDto>(await manager.Client.GetAsync($"/api/cash-shifts/{managerShift.Shift.Id}/report"));
        Assert.Equal(1, managerReport.ReturnCount);
        Assert.Equal(90m, managerReport.CashRefunds);
        Assert.Equal(410m, managerReport.ExpectedCash);               // 500 float - 90 paid back
        Assert.Equal(-90m, managerReport.NetSales);

        // the rest comes back in two parts and the refunds add up to the line, to the cent
        var second = await ReadAsync<QuickSaleDto>(await PostAsync(manager.Client, $"/api/quick-sales/{sale.Id}/returns",
            new ReturnQuickSaleRequest("faulty", "Card", [new ReturnItemRequest(lineId, 1m)])));
        var third = await ReadAsync<QuickSaleDto>(await PostAsync(manager.Client, $"/api/quick-sales/{sale.Id}/returns",
            new ReturnQuickSaleRequest("faulty", "Cash", [new ReturnItemRequest(lineId, 1m)])));
        Assert.Equal(270m, third.Returns.Sum(item => item.RefundTotal));
        Assert.Equal(10m, await StockAsync(code));

        using var tooMany = await PostAsync(manager.Client, $"/api/quick-sales/{sale.Id}/returns",
            new ReturnQuickSaleRequest("again", "Cash", [new ReturnItemRequest(lineId, 1m)]));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, tooMany.StatusCode);
        Assert.Equal(10m, await StockAsync(code));
        Assert.NotEmpty(second.Returns);
    }

    [Fact]
    [Trait("VUT", "09402")]
    public async Task AReturn_NeedsAnOpenShift_ARealLine_AndAReason_AndChangesNothingOtherwise()
    {
        using var manager = await _factory.ActorAsync("Manager");
        using var cashier = await _factory.ActorAsync("Technician");
        var code = NewCode();
        var product = await CreateProductAsync(manager, code, price: 100m, vat: 0m, stock: 10m);
        await PostAsync(cashier.Client, "/api/cash-shifts/open", new OpenShiftRequest(0m));
        var sale = await ReadAsync<QuickSaleDto>(await PostAsync(cashier.Client, "/api/quick-sales", SaleOf(product.Id, 2m, cash: 200m)));
        var lineId = sale.Lines.Single().Id;
        var counterBefore = await CounterAsync("quick-sale-return");

        using var noShift = await PostAsync(manager.Client, $"/api/quick-sales/{sale.Id}/returns",
            new ReturnQuickSaleRequest("faulty", "Cash", [new ReturnItemRequest(lineId, 1m)]));
        await AssertRejectedAsync(noShift, "SHIFT_REQUIRED");

        await PostAsync(manager.Client, "/api/cash-shifts/open", new OpenShiftRequest(0m));
        using var noReason = await PostAsync(manager.Client, $"/api/quick-sales/{sale.Id}/returns",
            new ReturnQuickSaleRequest(" ", "Cash", [new ReturnItemRequest(lineId, 1m)]));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, noReason.StatusCode);
        using var wrongLine = await PostAsync(manager.Client, $"/api/quick-sales/{sale.Id}/returns",
            new ReturnQuickSaleRequest("faulty", "Cash", [new ReturnItemRequest(Guid.NewGuid(), 1m)]));
        await AssertRejectedAsync(wrongLine, "RETURN_REJECTED");
        using var tooMany = await PostAsync(manager.Client, $"/api/quick-sales/{sale.Id}/returns",
            new ReturnQuickSaleRequest("faulty", "Cash", [new ReturnItemRequest(lineId, 3m)]));
        await AssertRejectedAsync(tooMany, "RETURN_REJECTED");
        using var badMethod = await PostAsync(manager.Client, $"/api/quick-sales/{sale.Id}/returns",
            new ReturnQuickSaleRequest("faulty", "Barter", [new ReturnItemRequest(lineId, 1m)]));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, badMethod.StatusCode);

        var untouched = await ReadAsync<QuickSaleDto>(await manager.Client.GetAsync($"/api/quick-sales/{sale.Id}"));
        Assert.Empty(untouched.Returns);
        Assert.Equal(0m, untouched.Lines.Single().ReturnedQuantity);
        Assert.Equal(8m, await StockAsync(code));
        Assert.Equal(counterBefore, await CounterAsync("quick-sale-return"));
    }

    // ---- looking sales up ------------------------------------------------------------------------------

    [Fact]
    [Trait("VUT", "09501")]
    public async Task Sales_AreListedAndSearched_AndACashierSeesOnlyTheirOwn()
    {
        using var manager = await _factory.ActorAsync("Manager");
        using var cashier = await _factory.ActorAsync("Technician");
        using var colleague = await _factory.ActorAsync("Technician");
        var product = await CreateProductAsync(manager, NewCode(), price: 10m, vat: 0m, stock: 50m);
        await PostAsync(cashier.Client, "/api/cash-shifts/open", new OpenShiftRequest(0m));
        await PostAsync(colleague.Client, "/api/cash-shifts/open", new OpenShiftRequest(0m));
        var mine = await ReadAsync<QuickSaleDto>(await PostAsync(cashier.Client, "/api/quick-sales", SaleOf(product.Id, 1m, cash: 10m)));
        var theirs = await ReadAsync<QuickSaleDto>(await PostAsync(colleague.Client, "/api/quick-sales", SaleOf(product.Id, 1m, cash: 10m)));

        var cashierList = await ReadAsync<List<QuickSaleSummaryDto>>(await cashier.Client.GetAsync("/api/quick-sales"));
        Assert.Contains(cashierList, sale => sale.Id == mine.Id);
        Assert.DoesNotContain(cashierList, sale => sale.Id == theirs.Id);

        using var notMine = await cashier.Client.GetAsync($"/api/quick-sales/{theirs.Id}");
        Assert.Equal(HttpStatusCode.NotFound, notMine.StatusCode);

        var managerList = await ReadAsync<List<QuickSaleSummaryDto>>(await manager.Client.GetAsync("/api/quick-sales?limit=100"));
        Assert.Contains(managerList, sale => sale.Id == mine.Id);
        Assert.Contains(managerList, sale => sale.Id == theirs.Id);

        var found = await ReadAsync<List<QuickSaleSummaryDto>>(await manager.Client.GetAsync($"/api/quick-sales?search={theirs.SaleNumber.ToLowerInvariant()}"));
        Assert.Equal(theirs.Id, Assert.Single(found).Id);

        var voidedOnly = await ReadAsync<List<QuickSaleSummaryDto>>(await manager.Client.GetAsync("/api/quick-sales?status=Voided"));
        Assert.DoesNotContain(voidedOnly, sale => sale.Id == mine.Id);
        using var badStatus = await manager.Client.GetAsync("/api/quick-sales?status=Nonsense");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, badStatus.StatusCode);

        var shifts = await ReadAsync<List<CashShiftDto>>(await cashier.Client.GetAsync("/api/cash-shifts"));
        Assert.All(shifts, shift => Assert.Equal(cashier.UserId, shift.CashierUserId));
    }

    // ---- price list ------------------------------------------------------------------------------------

    [Fact]
    [Trait("VUT", "04301")]
    public async Task Products_AreFoundByBarcodeOrCode_AndCodesAndBarcodesStayUnique()
    {
        using var manager = await _factory.ActorAsync("Manager");
        using var cashier = await _factory.ActorAsync("Technician");
        var code = NewCode();
        var barcode = $"869{Random.Shared.NextInt64(1_000_000_000L, 9_999_999_999L)}";
        var product = await CreateProductAsync(manager, code, price: 12.5m, vat: 10m, barcode: barcode, stock: 4m);
        Assert.Equal(4m, product.StockAvailable);

        var byBarcode = await ReadAsync<ProductDto>(await cashier.Client.GetAsync($"/api/products/lookup?term={barcode}"));
        var byCode = await ReadAsync<ProductDto>(await cashier.Client.GetAsync($"/api/products/lookup?term={code.ToLowerInvariant()}"));
        Assert.Equal(product.Id, byBarcode.Id);
        Assert.Equal(product.Id, byCode.Id);
        using var unknown = await cashier.Client.GetAsync("/api/products/lookup?term=does-not-exist");
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);

        using var sameCode = await PostAsync(manager.Client, "/api/products", new CreateProductRequest(code.ToLowerInvariant(), "Other", null, null, 1m, 20m, false));
        await AssertRejectedAsync(sameCode, "PRODUCT_CODE_EXISTS");
        using var sameBarcode = await PostAsync(manager.Client, "/api/products", new CreateProductRequest(NewCode(), "Other", barcode, null, 1m, 20m, false));
        await AssertRejectedAsync(sameBarcode, "PRODUCT_BARCODE_EXISTS");
        using var badVat = await PostAsync(manager.Client, "/api/products", new CreateProductRequest(NewCode(), "Other", null, null, 1m, 101m, false));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, badVat.StatusCode);
        using var noName = await PostAsync(manager.Client, "/api/products", new CreateProductRequest(NewCode(), " ", null, null, 1m, 20m, false));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, noName.StatusCode);

        var found = await ReadAsync<List<ProductDto>>(await cashier.Client.GetAsync($"/api/products?search={code.ToLowerInvariant()}"));
        Assert.Equal(product.Id, Assert.Single(found).Id);

        var renamed = await ReadAsync<ProductDto>(await PutAsync(manager.Client, $"/api/products/{product.Id}",
            new UpdateProductRequest("Renamed", null, "metre", 15m, 20m, true, true)));
        Assert.Equal((15m, "Renamed", "metre", null), (renamed.SalePrice, renamed.Name, renamed.Unit, renamed.Barcode));
        using var byOldBarcode = await cashier.Client.GetAsync($"/api/products/lookup?term={barcode}");
        Assert.Equal(HttpStatusCode.NotFound, byOldBarcode.StatusCode);
    }

    [Fact]
    [Trait("VUT", "04302")]
    public async Task ThePriceList_CanBeChangedByManagersOnly_AndTheCounterNeedsARole()
    {
        using var cashier = await _factory.ActorAsync("Technician");
        using var viewer = await _factory.ActorAsync("Viewer");
        using var anonymous = _factory.CreateApiClient();
        var request = new CreateProductRequest(NewCode(), "Nope", null, null, 1m, 20m, false);

        using var byCashier = await PostAsync(cashier.Client, "/api/products", request);
        Assert.Equal(HttpStatusCode.Forbidden, byCashier.StatusCode);

        using var listAsViewer = await viewer.Client.GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.Forbidden, listAsViewer.StatusCode);
        using var sellAsViewer = await PostAsync(viewer.Client, "/api/quick-sales", SaleOf(Guid.NewGuid(), 1m, cash: 1m));
        Assert.Equal(HttpStatusCode.Forbidden, sellAsViewer.StatusCode);
        using var shiftAsViewer = await PostAsync(viewer.Client, "/api/cash-shifts/open", new OpenShiftRequest(0m));
        Assert.Equal(HttpStatusCode.Forbidden, shiftAsViewer.StatusCode);

        using var listAnonymous = await anonymous.GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.Unauthorized, listAnonymous.StatusCode);
        using var salesAnonymous = await anonymous.GetAsync("/api/quick-sales");
        Assert.Equal(HttpStatusCode.Unauthorized, salesAnonymous.StatusCode);
    }

    // ---- helpers ---------------------------------------------------------------------------------------

    private static string NewCode() => $"P-{Guid.NewGuid():N}"[..14];

    private static CreateQuickSaleRequest SaleOf(Guid productId, decimal quantity, decimal cash, decimal lineDiscount = 0m, decimal receiptDiscount = 0m)
        => new(
            null,
            [new QuickSaleLineRequest(productId, null, quantity, null, null, lineDiscount)],
            receiptDiscount,
            [new QuickSalePaymentRequest("Cash", cash, null)],
            null);

    private async Task<ProductDto> CreateProductAsync(
        Actor manager, string code, decimal price = 120m, decimal vat = 20m, bool tracksStock = true, string? barcode = null, decimal stock = 10m)
    {
        if (tracksStock && stock > 0) await SeedStockAsync(code, stock);
        var response = await PostAsync(manager.Client, "/api/products", new CreateProductRequest(code, $"Product {code}", barcode, "adet", price, vat, tracksStock));
        return await ReadAsync<ProductDto>(response);
    }

    private async Task SeedStockAsync(string code, decimal quantity)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
        db.MaterialStocks.Add(new MaterialStock(Guid.NewGuid(), code, "Test stock", quantity));
        await db.SaveChangesAsync();
    }

    private async Task<decimal> StockAsync(string code)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
        return await db.MaterialStocks.AsNoTracking().Where(x => x.MaterialCode == code).Select(x => x.QuantityOnHand).SingleAsync();
    }

    private async Task<List<StockMovement>> MovementsAsync(string code)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
        return await db.StockMovements.AsNoTracking().Where(x => x.MaterialCode == code).ToListAsync();
    }

    private async Task<int> SaleCountAsync(Guid cashierUserId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
        return await db.QuickSales.AsNoTracking().CountAsync(x => x.CashierUserId == cashierUserId);
    }

    private async Task<long> CounterAsync(string key)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
        return await db.DocumentCounters.AsNoTracking().Where(x => x.Key == key).Select(x => x.LastValue).FirstOrDefaultAsync();
    }
}
