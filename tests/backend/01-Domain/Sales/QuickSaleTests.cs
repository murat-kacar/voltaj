using System;
using System.Linq;
using FluentAssertions;
using Voltflow.Domain.Common;
using Voltflow.Domain.Sales;
using Xunit;

namespace Voltflow.Tests.Domain.Sales;

public class QuickSaleTests
{
    private static QuickSaleLineInput Line(decimal quantity, decimal price, decimal vat = 20m, decimal discount = 0m, string code = "P1") =>
        new(null, code, null, "Item " + code, "adet", quantity, price, vat, discount, false);

    private static QuickSalePaymentInput Cash(decimal amount) => new(SalePaymentMethod.Cash, amount, null);

    private static QuickSalePaymentInput Card(decimal amount) => new(SalePaymentMethod.Card, amount, "slip-1");

    private static QuickSale Sale(QuickSaleLineInput[] lines, decimal receiptDiscount, params QuickSalePaymentInput[] payments) =>
        QuickSale.Create(() => "HS-000001", DateTime.UtcNow, Guid.NewGuid(), "Cashier", Guid.NewGuid(), null, lines, receiptDiscount, payments, null);

    // ---- totals, discount and VAT ---------------------------------------------------------------------

    [Fact]
    public void Create_ExtractsVatFromTheInclusivePrice()
    {
        var sale = Sale([Line(2, 120m)], 0m, Cash(240m));

        sale.Subtotal.Should().Be(240m);
        sale.GrandTotal.Should().Be(240m);
        sale.VatTotal.Should().Be(40m);
        sale.Lines.Single().VatAmount.Should().Be(40m);
    }

    [Fact]
    public void Create_TakesTheLineDiscountOffBeforeExtractingVat()
    {
        var sale = Sale([Line(1, 100m, discount: 20m)], 0m, Cash(80m));

        sale.Subtotal.Should().Be(100m);
        sale.LineDiscountTotal.Should().Be(20m);
        sale.GrandTotal.Should().Be(80m);
        sale.VatTotal.Should().Be(13.33m);
    }

    [Fact]
    public void Create_SpreadsTheReceiptDiscountByLineValue_AndItAddsUpToTheCent()
    {
        var sale = Sale([Line(1, 100m, code: "A"), Line(1, 50m, code: "B")], 10.01m, Cash(139.99m));

        sale.Lines.Select(line => line.LineNumber).Should().Equal(1, 2);
        sale.Lines.Select(line => line.ReceiptDiscountShare).Should().Equal(6.67m, 3.34m);
        sale.Lines.Select(line => line.LineTotal).Should().Equal(93.33m, 46.66m);
        sale.GrandTotal.Should().Be(139.99m);
        sale.ReceiptDiscount.Should().Be(10.01m);
    }

    [Fact]
    public void Create_HandsTheRoundingCentToALine_SoTheSharesNeverDriftFromTheDiscount()
    {
        var sale = Sale([Line(1, 10m, code: "A"), Line(1, 10m, code: "B"), Line(1, 10m, code: "C")], 10m, Cash(20m));

        sale.Lines.Sum(line => line.ReceiptDiscountShare).Should().Be(10m);
        sale.Lines.Should().OnlyContain(line => line.ReceiptDiscountShare <= 10m);
        sale.GrandTotal.Should().Be(20m);
    }

    [Fact]
    public void Create_RoundsAHalfCentAwayFromZero()
    {
        var sale = Sale([Line(3, 0.335m, vat: 0m)], 0m, Cash(1.01m));

        sale.GrandTotal.Should().Be(1.01m);
    }

    // ---- payments and change ---------------------------------------------------------------------------

    [Fact]
    public void Create_GivesChangeOnCash()
    {
        var sale = Sale([Line(2, 120m)], 0m, Cash(300m));

        sale.CashTendered.Should().Be(300m);
        sale.ChangeGiven.Should().Be(60m);
        var payment = sale.Payments.Single();
        payment.Method.Should().Be(SalePaymentMethod.Cash);
        payment.Amount.Should().Be(240m);
        payment.Tendered.Should().Be(300m);
    }

    [Fact]
    public void Create_AcceptsASplitBetweenCardAndCash_AndTheCashCoversTheRest()
    {
        var sale = Sale([Line(2, 120m)], 0m, Card(100m), Cash(200m));

        sale.Payments.Sum(payment => payment.Amount).Should().Be(240m);
        sale.Payments.Single(payment => payment.Method == SalePaymentMethod.Cash).Amount.Should().Be(140m);
        sale.ChangeGiven.Should().Be(60m);
    }

    [Fact]
    public void Create_AcceptsAnExactCardPayment_WithNoChange()
    {
        var sale = Sale([Line(1, 99.90m)], 0m, Card(99.90m));

        sale.ChangeGiven.Should().Be(0m);
        sale.CashTendered.Should().Be(0m);
    }

    [Fact]
    public void Create_RejectsCardOrTransferAboveTheTotal()
    {
        var act = () => Sale([Line(2, 120m)], 0m, Card(300m));

        act.Should().Throw<InvalidOperationException>().WithMessage("*cannot exceed the total*");
    }

    [Fact]
    public void Create_RejectsPaymentsThatDoNotCoverTheTotal()
    {
        var act = () => Sale([Line(2, 120m)], 0m, Card(100m), Cash(100m));

        act.Should().Throw<InvalidOperationException>().WithMessage("*do not cover the total*");
    }

    [Fact]
    public void Create_RejectsCashWhenNothingIsDueInCash()
    {
        var act = () => Sale([Line(2, 120m)], 0m, Card(240m), Cash(10m));

        act.Should().Throw<InvalidOperationException>().WithMessage("*Nothing is due in cash*");
    }

    [Fact]
    public void Create_RejectsAMissingOrZeroPayment()
    {
        var none = () => Sale([Line(1, 10m)], 0m);
        var zero = () => Sale([Line(1, 10m)], 0m, Cash(0m));

        none.Should().Throw<InvalidOperationException>();
        zero.Should().Throw<InvalidOperationException>();
    }

    // ---- input validation ------------------------------------------------------------------------------

    [Fact]
    public void Create_RejectsASaleWithoutLines()
    {
        var act = () => Sale([], 0m, Cash(10m));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Create_RejectsZeroQuantityAndNegativePrice()
    {
        var quantity = () => Sale([Line(0, 10m)], 0m, Cash(10m));
        var price = () => Sale([Line(1, -1m)], 0m, Cash(10m));

        quantity.Should().Throw<ArgumentException>();
        price.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_RejectsADiscountLargerThanTheLine_OrTheReceipt()
    {
        var line = () => Sale([Line(1, 10m, discount: 10.01m)], 0m, Cash(10m));
        var receipt = () => Sale([Line(1, 10m)], 10.01m, Cash(10m));
        var everything = () => Sale([Line(1, 10m)], 10m, Cash(10m));

        line.Should().Throw<InvalidOperationException>();
        receipt.Should().Throw<InvalidOperationException>();
        everything.Should().Throw<InvalidOperationException>().WithMessage("*greater than zero*");
    }

    [Fact]
    public void Create_TakesTheReceiptNumberOnceAfterEveryRulePassed_AndNeverForARejectedSale()
    {
        var taken = 0;
        string Next() { taken++; return "HS-000009"; }

        QuickSaleLineInput[] lines = [Line(1, 10m)];
        var rejected = () => QuickSale.Create(Next, DateTime.UtcNow, Guid.NewGuid(), "C", Guid.NewGuid(), null, lines, 0m, [Cash(5m)], null);
        rejected.Should().Throw<InvalidOperationException>();
        taken.Should().Be(0);

        var sale = QuickSale.Create(Next, DateTime.UtcNow, Guid.NewGuid(), "C", Guid.NewGuid(), null, lines, 0m, [Cash(10m)], null);
        sale.SaleNumber.Should().Be("HS-000009");
        taken.Should().Be(1);
    }

    // ---- void ------------------------------------------------------------------------------------------

    [Fact]
    public void Void_MarksTheSaleAndKeepsWhoAndWhy()
    {
        var sale = Sale([Line(1, 10m)], 0m, Cash(10m));
        var user = Guid.NewGuid();

        sale.Void(user, "  wrong item  ", new DateTime(2030, 1, 2, 3, 4, 5, DateTimeKind.Utc));

        sale.Status.Should().Be(QuickSaleStatus.Voided);
        sale.VoidedByUserId.Should().Be(user);
        sale.VoidReason.Should().Be("wrong item");
        sale.VoidedAt.Should().Be(new DateTime(2030, 1, 2, 3, 4, 5, DateTimeKind.Utc));
    }

    [Fact]
    public void Void_RequiresAReason_AndCannotRunTwice()
    {
        var sale = Sale([Line(1, 10m)], 0m, Cash(10m));

        var noReason = () => sale.Void(Guid.NewGuid(), " ", DateTime.UtcNow);
        noReason.Should().Throw<ArgumentException>();

        sale.Void(Guid.NewGuid(), "duplicate", DateTime.UtcNow);
        var twice = () => sale.Void(Guid.NewGuid(), "again", DateTime.UtcNow);
        twice.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Void_IsRefusedOnceAnythingWasReturned()
    {
        var sale = Sale([Line(2, 10m)], 0m, Cash(20m));
        Return(sale, (sale.Lines.Single().Id, 1m));

        var act = () => sale.Void(Guid.NewGuid(), "too late", DateTime.UtcNow);

        act.Should().Throw<InvalidOperationException>().WithMessage("*has returns*");
    }

    // ---- returns ---------------------------------------------------------------------------------------

    private static QuickSaleReturn Return(QuickSale sale, params (Guid LineId, decimal Quantity)[] items) =>
        QuickSaleReturn.Create(() => "IA-000001", sale, DateTime.UtcNow, Guid.NewGuid(), "Cashier", Guid.NewGuid(), "customer changed their mind", SalePaymentMethod.Cash, items);

    [Fact]
    public void Return_RefundsExactlyTheLineTotal_WhenAThirdComesBackThreeTimes()
    {
        // 3 x 33.3333 = 100.00: a naive refund of 33.33 each time would give back only 99.99.
        var sale = Sale([Line(3, 33.3333m, vat: 0m)], 0m, Cash(100m));
        var lineId = sale.Lines.Single().Id;

        var first = Return(sale, (lineId, 1m)).RefundTotal;
        var second = Return(sale, (lineId, 1m)).RefundTotal;
        var third = Return(sale, (lineId, 1m)).RefundTotal;

        (first + second + third).Should().Be(100m);
        first.Should().Be(33.33m);
        second.Should().Be(33.34m);
        third.Should().Be(33.33m);
        sale.Lines.Single().ReturnedQuantity.Should().Be(3m);
    }

    [Fact]
    public void Return_UsesWhatWasActuallyPaidForTheLine_AfterDiscounts()
    {
        var sale = Sale([Line(2, 50m, discount: 10m)], 0m, Cash(90m));

        var refund = Return(sale, (sale.Lines.Single().Id, 1m)).RefundTotal;

        refund.Should().Be(45m);
    }

    [Fact]
    public void Return_CannotTakeBackMoreThanWasSold()
    {
        var sale = Sale([Line(2, 10m)], 0m, Cash(20m));
        var lineId = sale.Lines.Single().Id;
        Return(sale, (lineId, 2m));

        var act = () => Return(sale, (lineId, 1m));

        act.Should().Throw<InvalidOperationException>().WithMessage("*more than was sold*");
    }

    [Fact]
    public void Return_RejectsUnknownLines_DuplicateLines_AndAnEmptyReturn()
    {
        var sale = Sale([Line(2, 10m)], 0m, Cash(20m));
        var lineId = sale.Lines.Single().Id;

        var unknown = () => Return(sale, (Guid.NewGuid(), 1m));
        var duplicate = () => Return(sale, (lineId, 1m), (lineId, 1m));
        var empty = () => Return(sale);

        unknown.Should().Throw<InvalidOperationException>();
        duplicate.Should().Throw<InvalidOperationException>();
        empty.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Return_ChangesNothing_WhenALaterLineIsInvalid()
    {
        // The command journal saves the shared context even when a request fails, so a rejected return must not
        // have touched the lines that came before the bad one.
        var sale = Sale([Line(2, 10m, code: "A"), Line(1, 10m, code: "B")], 0m, Cash(30m));
        var first = sale.Lines.First().Id;
        var second = sale.Lines.Last().Id;

        var act = () => Return(sale, (first, 1m), (second, 5m));

        act.Should().Throw<InvalidOperationException>().WithMessage("*more than was sold*");
        sale.Lines.Should().OnlyContain(line => line.ReturnedQuantity == 0m);
    }

    [Fact]
    public void Void_ChangesNothing_WhenTheReasonIsMissing()
    {
        var sale = Sale([Line(1, 10m)], 0m, Cash(10m));

        var act = () => sale.Void(Guid.NewGuid(), " ", DateTime.UtcNow);

        act.Should().Throw<ArgumentException>();
        sale.Status.Should().Be(QuickSaleStatus.Completed);
        sale.VoidedAt.Should().BeNull();
    }

    [Fact]
    public void Return_IsRefusedForAVoidedSale_AndNeedsAReason()
    {
        var sale = Sale([Line(1, 10m)], 0m, Cash(10m));
        var lineId = sale.Lines.Single().Id;

        var noReason = () => QuickSaleReturn.Create(() => "IA-000002", sale, DateTime.UtcNow, Guid.NewGuid(), "C", Guid.NewGuid(), " ", SalePaymentMethod.Cash, [(lineId, 1m)]);
        noReason.Should().Throw<ArgumentException>();

        sale.Void(Guid.NewGuid(), "mistake", DateTime.UtcNow);
        var voided = () => Return(sale, (lineId, 1m));
        voided.Should().Throw<InvalidOperationException>().WithMessage("*completed sale*");
    }

    [Fact]
    public void Return_NeverTakesANumberForARejectedReturn()
    {
        var sale = Sale([Line(1, 10m)], 0m, Cash(10m));
        var lineId = sale.Lines.Single().Id;
        var taken = 0;
        string Next() { taken++; return "IA-000007"; }

        var tooMany = () => QuickSaleReturn.Create(Next, sale, DateTime.UtcNow, Guid.NewGuid(), "C", Guid.NewGuid(), "reason", SalePaymentMethod.Cash, [(lineId, 2m)]);
        tooMany.Should().Throw<InvalidOperationException>();
        taken.Should().Be(0);

        var accepted = QuickSaleReturn.Create(Next, sale, DateTime.UtcNow, Guid.NewGuid(), "C", Guid.NewGuid(), "reason", SalePaymentMethod.Cash, [(lineId, 1m)]);
        accepted.ReturnNumber.Should().Be("IA-000007");
        taken.Should().Be(1);
    }

    // ---- shift and counter -----------------------------------------------------------------------------

    [Fact]
    public void Shift_ClosesWithTheDifferenceBetweenCountedAndExpectedCash()
    {
        var shift = CashShift.Open(Guid.NewGuid(), "Cashier", 100m, new DateTime(2030, 1, 1, 8, 0, 0, DateTimeKind.Utc));

        shift.Close(Guid.NewGuid(), countedCash: 345m, expectedCash: 350m, note: "  short by five  ", new DateTime(2030, 1, 1, 18, 0, 0, DateTimeKind.Utc));

        shift.Status.Should().Be(CashShiftStatus.Closed);
        shift.CashDifference.Should().Be(-5m);
        shift.Note.Should().Be("short by five");
        shift.ClosedAt.Should().Be(new DateTime(2030, 1, 1, 18, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Shift_CannotBeClosedTwice_OrOpenedWithANegativeFloat()
    {
        var shift = CashShift.Open(Guid.NewGuid(), "Cashier", 0m, DateTime.UtcNow);
        shift.Close(Guid.NewGuid(), 0m, 0m, null, DateTime.UtcNow);

        var twice = () => shift.Close(Guid.NewGuid(), 0m, 0m, null, DateTime.UtcNow);
        var negative = () => CashShift.Open(Guid.NewGuid(), "Cashier", -1m, DateTime.UtcNow);

        twice.Should().Throw<InvalidOperationException>();
        negative.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DocumentCounter_HandsOutConsecutiveNumbers()
    {
        var counter = new DocumentCounter("QuickSale");

        counter.Next().Should().Be(1);
        counter.Next().Should().Be(2);
        counter.LastValue.Should().Be(2);
    }
}
