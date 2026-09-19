using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voltflow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuickSale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CashShifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CashierUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CashierName = table.Column<string>(type: "text", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OpeningCash = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CountedCash = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    ExpectedCash = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    CashDifference = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Note = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByEndpoint = table.Column<string>(type: "text", nullable: true),
                    UpdatedByEndpoint = table.Column<string>(type: "text", nullable: true),
                    CreatedInOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedInOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashShifts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentCounters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    LastValue = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByEndpoint = table.Column<string>(type: "text", nullable: true),
                    UpdatedByEndpoint = table.Column<string>(type: "text", nullable: true),
                    CreatedInOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedInOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentCounters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Barcode = table.Column<string>(type: "text", nullable: true),
                    Unit = table.Column<string>(type: "text", nullable: false),
                    SalePrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    VatRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    TracksStock = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByEndpoint = table.Column<string>(type: "text", nullable: true),
                    UpdatedByEndpoint = table.Column<string>(type: "text", nullable: true),
                    CreatedInOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedInOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuickSales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SaleNumber = table.Column<string>(type: "text", nullable: false),
                    SoldAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CashierUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CashierName = table.Column<string>(type: "text", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LineDiscountTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ReceiptDiscount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    GrandTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    VatTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CashTendered = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ChangeGiven = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    VoidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VoidedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    VoidReason = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByEndpoint = table.Column<string>(type: "text", nullable: true),
                    UpdatedByEndpoint = table.Column<string>(type: "text", nullable: true),
                    CreatedInOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedInOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuickSales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuickSales_CashShifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "CashShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuickSales_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuickSaleLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuickSaleId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductCode = table.Column<string>(type: "text", nullable: true),
                    Barcode = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Unit = table.Column<string>(type: "text", nullable: false),
                    TracksStock = table.Column<bool>(type: "boolean", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    VatRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    LineDiscount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ReceiptDiscountShare = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    VatAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ReturnedQuantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByEndpoint = table.Column<string>(type: "text", nullable: true),
                    UpdatedByEndpoint = table.Column<string>(type: "text", nullable: true),
                    CreatedInOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedInOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuickSaleLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuickSaleLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuickSaleLines_QuickSales_QuickSaleId",
                        column: x => x.QuickSaleId,
                        principalTable: "QuickSales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuickSalePayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuickSaleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Method = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Tendered = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Reference = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByEndpoint = table.Column<string>(type: "text", nullable: true),
                    UpdatedByEndpoint = table.Column<string>(type: "text", nullable: true),
                    CreatedInOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedInOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuickSalePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuickSalePayments_QuickSales_QuickSaleId",
                        column: x => x.QuickSaleId,
                        principalTable: "QuickSales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuickSaleReturns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReturnNumber = table.Column<string>(type: "text", nullable: false),
                    QuickSaleId = table.Column<Guid>(type: "uuid", nullable: false),
                    SaleNumber = table.Column<string>(type: "text", nullable: false),
                    ReturnedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CashierUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CashierName = table.Column<string>(type: "text", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    RefundMethod = table.Column<string>(type: "text", nullable: false),
                    RefundTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByEndpoint = table.Column<string>(type: "text", nullable: true),
                    UpdatedByEndpoint = table.Column<string>(type: "text", nullable: true),
                    CreatedInOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedInOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuickSaleReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuickSaleReturns_CashShifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "CashShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuickSaleReturns_QuickSales_QuickSaleId",
                        column: x => x.QuickSaleId,
                        principalTable: "QuickSales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuickSaleReturnLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuickSaleReturnId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuickSaleLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductCode = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    TracksStock = table.Column<bool>(type: "boolean", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RefundAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByEndpoint = table.Column<string>(type: "text", nullable: true),
                    UpdatedByEndpoint = table.Column<string>(type: "text", nullable: true),
                    CreatedInOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedInOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuickSaleReturnLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuickSaleReturnLines_QuickSaleLines_QuickSaleLineId",
                        column: x => x.QuickSaleLineId,
                        principalTable: "QuickSaleLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuickSaleReturnLines_QuickSaleReturns_QuickSaleReturnId",
                        column: x => x.QuickSaleReturnId,
                        principalTable: "QuickSaleReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashShifts_OneOpenPerCashier",
                table: "CashShifts",
                column: "CashierUserId",
                unique: true,
                filter: "\"Status\" = 'Open'");

            migrationBuilder.CreateIndex(
                name: "IX_CashShifts_OpenedAt",
                table: "CashShifts",
                column: "OpenedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentCounters_Key",
                table: "DocumentCounters",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_Barcode",
                table: "Products",
                column: "Barcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_Code",
                table: "Products",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuickSaleLines_ProductId",
                table: "QuickSaleLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_QuickSaleLines_QuickSaleId",
                table: "QuickSaleLines",
                column: "QuickSaleId");

            migrationBuilder.CreateIndex(
                name: "IX_QuickSalePayments_QuickSaleId",
                table: "QuickSalePayments",
                column: "QuickSaleId");

            migrationBuilder.CreateIndex(
                name: "IX_QuickSaleReturnLines_QuickSaleLineId",
                table: "QuickSaleReturnLines",
                column: "QuickSaleLineId");

            migrationBuilder.CreateIndex(
                name: "IX_QuickSaleReturnLines_QuickSaleReturnId",
                table: "QuickSaleReturnLines",
                column: "QuickSaleReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_QuickSaleReturns_QuickSaleId",
                table: "QuickSaleReturns",
                column: "QuickSaleId");

            migrationBuilder.CreateIndex(
                name: "IX_QuickSaleReturns_ReturnNumber",
                table: "QuickSaleReturns",
                column: "ReturnNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuickSaleReturns_ShiftId",
                table: "QuickSaleReturns",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_QuickSales_CustomerId",
                table: "QuickSales",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_QuickSales_SaleNumber",
                table: "QuickSales",
                column: "SaleNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuickSales_ShiftId",
                table: "QuickSales",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_QuickSales_SoldAt",
                table: "QuickSales",
                column: "SoldAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentCounters");

            migrationBuilder.DropTable(
                name: "QuickSalePayments");

            migrationBuilder.DropTable(
                name: "QuickSaleReturnLines");

            migrationBuilder.DropTable(
                name: "QuickSaleLines");

            migrationBuilder.DropTable(
                name: "QuickSaleReturns");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "QuickSales");

            migrationBuilder.DropTable(
                name: "CashShifts");
        }
    }
}
