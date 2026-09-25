using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voltflow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LiquidateModulesAndAddService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuickSaleLines_Products_ProductId",
                table: "QuickSaleLines");

            migrationBuilder.DropForeignKey(
                name: "FK_QuickSaleLines_QuickSales_QuickSaleId",
                table: "QuickSaleLines");

            migrationBuilder.DropForeignKey(
                name: "FK_QuickSalePayments_QuickSales_QuickSaleId",
                table: "QuickSalePayments");

            migrationBuilder.DropForeignKey(
                name: "FK_QuickSaleReturnLines_QuickSaleLines_QuickSaleLineId",
                table: "QuickSaleReturnLines");

            migrationBuilder.DropForeignKey(
                name: "FK_QuickSaleReturnLines_QuickSaleReturns_QuickSaleReturnId",
                table: "QuickSaleReturnLines");

            migrationBuilder.DropForeignKey(
                name: "FK_QuickSaleReturns_CashShifts_ShiftId",
                table: "QuickSaleReturns");

            migrationBuilder.DropForeignKey(
                name: "FK_QuickSaleReturns_QuickSales_QuickSaleId",
                table: "QuickSaleReturns");

            migrationBuilder.DropForeignKey(
                name: "FK_QuickSales_CashShifts_ShiftId",
                table: "QuickSales");

            migrationBuilder.DropForeignKey(
                name: "FK_QuickSales_Customers_CustomerId",
                table: "QuickSales");

            migrationBuilder.DropTable(
                name: "BillingEntries");

            migrationBuilder.DropTable(
                name: "MaintenanceContracts");

            migrationBuilder.DropTable(
                name: "ProjectPhase");

            migrationBuilder.DropTable(
                name: "WorkOrderItem");

            migrationBuilder.DropTable(
                name: "WorkOrderTimeEntry");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "WorkOrders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_QuickSales",
                table: "QuickSales");

            migrationBuilder.DropPrimaryKey(
                name: "PK_QuickSaleReturns",
                table: "QuickSaleReturns");

            migrationBuilder.DropPrimaryKey(
                name: "PK_QuickSaleReturnLines",
                table: "QuickSaleReturnLines");

            migrationBuilder.DropPrimaryKey(
                name: "PK_QuickSalePayments",
                table: "QuickSalePayments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_QuickSaleLines",
                table: "QuickSaleLines");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CashShifts",
                table: "CashShifts");

            migrationBuilder.RenameTable(
                name: "QuickSales",
                newName: "QuickSale");

            migrationBuilder.RenameTable(
                name: "QuickSaleReturns",
                newName: "QuickSaleReturn");

            migrationBuilder.RenameTable(
                name: "QuickSaleReturnLines",
                newName: "QuickSaleReturnLine");

            migrationBuilder.RenameTable(
                name: "QuickSalePayments",
                newName: "QuickSalePayment");

            migrationBuilder.RenameTable(
                name: "QuickSaleLines",
                newName: "QuickSaleLine");

            migrationBuilder.RenameTable(
                name: "CashShifts",
                newName: "CashShift");

            migrationBuilder.RenameColumn(
                name: "ProjectId",
                table: "ProgressBillings",
                newName: "ServiceId");

            migrationBuilder.RenameIndex(
                name: "IX_ProgressBillings_ProjectId_BillingNumber",
                table: "ProgressBillings",
                newName: "IX_ProgressBillings_ServiceId_BillingNumber");

            migrationBuilder.RenameIndex(
                name: "IX_QuickSales_SoldAt",
                table: "QuickSale",
                newName: "IX_QuickSale_SoldAt");

            migrationBuilder.RenameIndex(
                name: "IX_QuickSales_ShiftId",
                table: "QuickSale",
                newName: "IX_QuickSale_ShiftId");

            migrationBuilder.RenameIndex(
                name: "IX_QuickSales_SaleNumber",
                table: "QuickSale",
                newName: "IX_QuickSale_SaleNumber");

            migrationBuilder.RenameIndex(
                name: "IX_QuickSales_CustomerId",
                table: "QuickSale",
                newName: "IX_QuickSale_CustomerId");

            migrationBuilder.RenameIndex(
                name: "IX_QuickSaleReturns_ShiftId",
                table: "QuickSaleReturn",
                newName: "IX_QuickSaleReturn_ShiftId");

            migrationBuilder.RenameIndex(
                name: "IX_QuickSaleReturns_ReturnNumber",
                table: "QuickSaleReturn",
                newName: "IX_QuickSaleReturn_ReturnNumber");

            migrationBuilder.RenameIndex(
                name: "IX_QuickSaleReturns_QuickSaleId",
                table: "QuickSaleReturn",
                newName: "IX_QuickSaleReturn_QuickSaleId");

            migrationBuilder.RenameIndex(
                name: "IX_QuickSaleReturnLines_QuickSaleReturnId",
                table: "QuickSaleReturnLine",
                newName: "IX_QuickSaleReturnLine_QuickSaleReturnId");

            migrationBuilder.RenameIndex(
                name: "IX_QuickSaleReturnLines_QuickSaleLineId",
                table: "QuickSaleReturnLine",
                newName: "IX_QuickSaleReturnLine_QuickSaleLineId");

            migrationBuilder.RenameIndex(
                name: "IX_QuickSalePayments_QuickSaleId",
                table: "QuickSalePayment",
                newName: "IX_QuickSalePayment_QuickSaleId");

            migrationBuilder.RenameIndex(
                name: "IX_QuickSaleLines_QuickSaleId",
                table: "QuickSaleLine",
                newName: "IX_QuickSaleLine_QuickSaleId");

            migrationBuilder.RenameIndex(
                name: "IX_QuickSaleLines_ProductId",
                table: "QuickSaleLine",
                newName: "IX_QuickSaleLine_ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_CashShifts_OpenedAt",
                table: "CashShift",
                newName: "IX_CashShift_OpenedAt");

            migrationBuilder.AddPrimaryKey(
                name: "PK_QuickSale",
                table: "QuickSale",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_QuickSaleReturn",
                table: "QuickSaleReturn",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_QuickSaleReturnLine",
                table: "QuickSaleReturnLine",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_QuickSalePayment",
                table: "QuickSalePayment",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_QuickSaleLine",
                table: "QuickSaleLine",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CashShift",
                table: "CashShift",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_QuickSale_CashShift_ShiftId",
                table: "QuickSale",
                column: "ShiftId",
                principalTable: "CashShift",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuickSale_Customers_CustomerId",
                table: "QuickSale",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuickSaleLine_Products_ProductId",
                table: "QuickSaleLine",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuickSaleLine_QuickSale_QuickSaleId",
                table: "QuickSaleLine",
                column: "QuickSaleId",
                principalTable: "QuickSale",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QuickSalePayment_QuickSale_QuickSaleId",
                table: "QuickSalePayment",
                column: "QuickSaleId",
                principalTable: "QuickSale",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QuickSaleReturn_CashShift_ShiftId",
                table: "QuickSaleReturn",
                column: "ShiftId",
                principalTable: "CashShift",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuickSaleReturn_QuickSale_QuickSaleId",
                table: "QuickSaleReturn",
                column: "QuickSaleId",
                principalTable: "QuickSale",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuickSaleReturnLine_QuickSaleLine_QuickSaleLineId",
                table: "QuickSaleReturnLine",
                column: "QuickSaleLineId",
                principalTable: "QuickSaleLine",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuickSaleReturnLine_QuickSaleReturn_QuickSaleReturnId",
                table: "QuickSaleReturnLine",
                column: "QuickSaleReturnId",
                principalTable: "QuickSaleReturn",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuickSale_CashShift_ShiftId",
                table: "QuickSale");

            migrationBuilder.DropForeignKey(
                name: "FK_QuickSale_Customers_CustomerId",
                table: "QuickSale");

            migrationBuilder.DropForeignKey(
                name: "FK_QuickSaleLine_Products_ProductId",
                table: "QuickSaleLine");

            migrationBuilder.DropForeignKey(
                name: "FK_QuickSaleLine_QuickSale_QuickSaleId",
                table: "QuickSaleLine");

            migrationBuilder.DropForeignKey(
                name: "FK_QuickSalePayment_QuickSale_QuickSaleId",
                table: "QuickSalePayment");

            migrationBuilder.DropForeignKey(
                name: "FK_QuickSaleReturn_CashShift_ShiftId",
                table: "QuickSaleReturn");

            migrationBuilder.DropForeignKey(
                name: "FK_QuickSaleReturn_QuickSale_QuickSaleId",
                table: "QuickSaleReturn");

            migrationBuilder.DropForeignKey(
                name: "FK_QuickSaleReturnLine_QuickSaleLine_QuickSaleLineId",
                table: "QuickSaleReturnLine");

            migrationBuilder.DropForeignKey(
                name: "FK_QuickSaleReturnLine_QuickSaleReturn_QuickSaleReturnId",
                table: "QuickSaleReturnLine");

            migrationBuilder.DropPrimaryKey(
                name: "PK_QuickSaleReturnLine",
                table: "QuickSaleReturnLine");

            migrationBuilder.DropPrimaryKey(
                name: "PK_QuickSaleReturn",
                table: "QuickSaleReturn");

            migrationBuilder.DropPrimaryKey(
                name: "PK_QuickSalePayment",
                table: "QuickSalePayment");

            migrationBuilder.DropPrimaryKey(
                name: "PK_QuickSaleLine",
                table: "QuickSaleLine");

            migrationBuilder.DropPrimaryKey(
                name: "PK_QuickSale",
                table: "QuickSale");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CashShift",
                table: "CashShift");

            migrationBuilder.RenameTable(
                name: "QuickSaleReturnLine",
                newName: "QuickSaleReturnLines");

            migrationBuilder.RenameTable(
                name: "QuickSaleReturn",
                newName: "QuickSaleReturns");

            migrationBuilder.RenameTable(
                name: "QuickSalePayment",
                newName: "QuickSalePayments");

            migrationBuilder.RenameTable(
                name: "QuickSaleLine",
                newName: "QuickSaleLines");

            migrationBuilder.RenameTable(
                name: "QuickSale",
                newName: "QuickSales");

            migrationBuilder.RenameTable(
                name: "CashShift",
                newName: "CashShifts");

            migrationBuilder.RenameColumn(
                name: "ServiceId",
                table: "ProgressBillings",
                newName: "ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_ProgressBillings_ServiceId_BillingNumber",
                table: "ProgressBillings",
                newName: "IX_ProgressBillings_ProjectId_BillingNumber");

            migrationBuilder.RenameIndex(
                name: "IX_QuickSaleReturnLine_QuickSaleReturnId",
                table: "QuickSaleReturnLines",
                newName: "IX_QuickSaleReturnLines_QuickSaleReturnId");

            migrationBuilder.RenameIndex(
                name: "IX_QuickSaleReturnLine_QuickSaleLineId",
                table: "QuickSaleReturnLines",
                newName: "IX_QuickSaleReturnLines_QuickSaleLineId");

            migrationBuilder.RenameIndex(
                name: "IX_QuickSaleReturn_ShiftId",
                table: "QuickSaleReturns",
                newName: "IX_QuickSaleReturns_ShiftId");

            migrationBuilder.RenameIndex(
                name: "IX_QuickSaleReturn_ReturnNumber",
                table: "QuickSaleReturns",
                newName: "IX_QuickSaleReturns_ReturnNumber");

            migrationBuilder.RenameIndex(
                name: "IX_QuickSaleReturn_QuickSaleId",
                table: "QuickSaleReturns",
                newName: "IX_QuickSaleReturns_QuickSaleId");

            migrationBuilder.RenameIndex(
                name: "IX_QuickSalePayment_QuickSaleId",
                table: "QuickSalePayments",
                newName: "IX_QuickSalePayments_QuickSaleId");

            migrationBuilder.RenameIndex(
                name: "IX_QuickSaleLine_QuickSaleId",
                table: "QuickSaleLines",
                newName: "IX_QuickSaleLines_QuickSaleId");

            migrationBuilder.RenameIndex(
                name: "IX_QuickSaleLine_ProductId",
                table: "QuickSaleLines",
                newName: "IX_QuickSaleLines_ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_QuickSale_SoldAt",
                table: "QuickSales",
                newName: "IX_QuickSales_SoldAt");

            migrationBuilder.RenameIndex(
                name: "IX_QuickSale_ShiftId",
                table: "QuickSales",
                newName: "IX_QuickSales_ShiftId");

            migrationBuilder.RenameIndex(
                name: "IX_QuickSale_SaleNumber",
                table: "QuickSales",
                newName: "IX_QuickSales_SaleNumber");

            migrationBuilder.RenameIndex(
                name: "IX_QuickSale_CustomerId",
                table: "QuickSales",
                newName: "IX_QuickSales_CustomerId");

            migrationBuilder.RenameIndex(
                name: "IX_CashShift_OpenedAt",
                table: "CashShifts",
                newName: "IX_CashShifts_OpenedAt");

            migrationBuilder.AddPrimaryKey(
                name: "PK_QuickSaleReturnLines",
                table: "QuickSaleReturnLines",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_QuickSaleReturns",
                table: "QuickSaleReturns",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_QuickSalePayments",
                table: "QuickSalePayments",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_QuickSaleLines",
                table: "QuickSaleLines",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_QuickSales",
                table: "QuickSales",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CashShifts",
                table: "CashShifts",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "BillingEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByEndpoint = table.Column<string>(type: "text", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedInOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByEndpoint = table.Column<string>(type: "text", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedInOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceContracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByEndpoint = table.Column<string>(type: "text", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedInOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    FrequencyMonths = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    NextMaintenanceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByEndpoint = table.Column<string>(type: "text", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedInOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceContracts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Budget = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByEndpoint = table.Column<string>(type: "text", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedInOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Number = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByEndpoint = table.Column<string>(type: "text", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedInOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CancellationReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByEndpoint = table.Column<string>(type: "text", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedInOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    HoldReason = table.Column<string>(type: "text", nullable: true),
                    IsSafetyChecklistCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    Number = table.Column<string>(type: "text", nullable: false),
                    ParentWorkOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    PhaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProofOfWorkPhotoUrl = table.Column<string>(type: "text", nullable: true),
                    SignatureData = table.Column<string>(type: "text", nullable: true),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceServiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TargetCompletionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByEndpoint = table.Column<string>(type: "text", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedInOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectPhase",
                columns: table => new
                {
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlannedAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectPhase", x => new { x.ProjectId, x.Id });
                    table.ForeignKey(
                        name: "FK_ProjectPhase_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrderItem",
                columns: table => new
                {
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderItem", x => new { x.WorkOrderId, x.Id });
                    table.ForeignKey(
                        name: "FK_WorkOrderItem_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrderTimeEntry",
                columns: table => new
                {
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckInTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CheckOutTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderTimeEntry", x => new { x.WorkOrderId, x.Id });
                    table.ForeignKey(
                        name: "FK_WorkOrderTimeEntry_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_AssignedUserId",
                table: "WorkOrders",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_SourceServiceId",
                table: "WorkOrders",
                column: "SourceServiceId",
                unique: true,
                filter: "\"SourceServiceId\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_QuickSaleLines_Products_ProductId",
                table: "QuickSaleLines",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuickSaleLines_QuickSales_QuickSaleId",
                table: "QuickSaleLines",
                column: "QuickSaleId",
                principalTable: "QuickSales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QuickSalePayments_QuickSales_QuickSaleId",
                table: "QuickSalePayments",
                column: "QuickSaleId",
                principalTable: "QuickSales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QuickSaleReturnLines_QuickSaleLines_QuickSaleLineId",
                table: "QuickSaleReturnLines",
                column: "QuickSaleLineId",
                principalTable: "QuickSaleLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuickSaleReturnLines_QuickSaleReturns_QuickSaleReturnId",
                table: "QuickSaleReturnLines",
                column: "QuickSaleReturnId",
                principalTable: "QuickSaleReturns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QuickSaleReturns_CashShifts_ShiftId",
                table: "QuickSaleReturns",
                column: "ShiftId",
                principalTable: "CashShifts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuickSaleReturns_QuickSales_QuickSaleId",
                table: "QuickSaleReturns",
                column: "QuickSaleId",
                principalTable: "QuickSales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuickSales_CashShifts_ShiftId",
                table: "QuickSales",
                column: "ShiftId",
                principalTable: "CashShifts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuickSales_Customers_CustomerId",
                table: "QuickSales",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
