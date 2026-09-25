using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voltflow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OperationAuditTracing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByEndpoint",
                table: "WorkOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedInOperationId",
                table: "WorkOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByEndpoint",
                table: "WorkOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "WorkOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedInOperationId",
                table: "WorkOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByEndpoint",
                table: "StockMovements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedInOperationId",
                table: "StockMovements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByEndpoint",
                table: "StockMovements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "StockMovements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedInOperationId",
                table: "StockMovements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByEndpoint",
                table: "SalesInvoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedInOperationId",
                table: "SalesInvoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByEndpoint",
                table: "SalesInvoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "SalesInvoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedInOperationId",
                table: "SalesInvoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByEndpoint",
                table: "ReminderRecords",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedInOperationId",
                table: "ReminderRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByEndpoint",
                table: "ReminderRecords",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "ReminderRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedInOperationId",
                table: "ReminderRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByEndpoint",
                table: "ReferenceValues",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedInOperationId",
                table: "ReferenceValues",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByEndpoint",
                table: "ReferenceValues",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "ReferenceValues",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedInOperationId",
                table: "ReferenceValues",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByEndpoint",
                table: "Quotes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedInOperationId",
                table: "Quotes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByEndpoint",
                table: "Quotes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "Quotes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedInOperationId",
                table: "Quotes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByEndpoint",
                table: "Projects",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedInOperationId",
                table: "Projects",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByEndpoint",
                table: "Projects",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "Projects",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedInOperationId",
                table: "Projects",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByEndpoint",
                table: "ProgressBillings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedInOperationId",
                table: "ProgressBillings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByEndpoint",
                table: "ProgressBillings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "ProgressBillings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedInOperationId",
                table: "ProgressBillings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByEndpoint",
                table: "PaymentInvoiceAllocations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedInOperationId",
                table: "PaymentInvoiceAllocations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByEndpoint",
                table: "PaymentInvoiceAllocations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "PaymentInvoiceAllocations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedInOperationId",
                table: "PaymentInvoiceAllocations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByEndpoint",
                table: "MaterialStocks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedInOperationId",
                table: "MaterialStocks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByEndpoint",
                table: "MaterialStocks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "MaterialStocks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedInOperationId",
                table: "MaterialStocks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByEndpoint",
                table: "ExecutionGuards",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedInOperationId",
                table: "ExecutionGuards",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByEndpoint",
                table: "ExecutionGuards",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "ExecutionGuards",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedInOperationId",
                table: "ExecutionGuards",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByEndpoint",
                table: "Customers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedInOperationId",
                table: "Customers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByEndpoint",
                table: "Customers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "Customers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedInOperationId",
                table: "Customers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByEndpoint",
                table: "CustomerPayments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedInOperationId",
                table: "CustomerPayments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByEndpoint",
                table: "CustomerPayments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "CustomerPayments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedInOperationId",
                table: "CustomerPayments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByEndpoint",
                table: "CustomerLedgerEntries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedInOperationId",
                table: "CustomerLedgerEntries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByEndpoint",
                table: "CustomerLedgerEntries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "CustomerLedgerEntries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedInOperationId",
                table: "CustomerLedgerEntries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByEndpoint",
                table: "CustomerConversions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedInOperationId",
                table: "CustomerConversions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByEndpoint",
                table: "CustomerConversions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "CustomerConversions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedInOperationId",
                table: "CustomerConversions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByEndpoint",
                table: "CandidateCustomers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedInOperationId",
                table: "CandidateCustomers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByEndpoint",
                table: "CandidateCustomers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "CandidateCustomers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedInOperationId",
                table: "CandidateCustomers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByEndpoint",
                table: "BillingEntries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedInOperationId",
                table: "BillingEntries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByEndpoint",
                table: "BillingEntries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "BillingEntries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedInOperationId",
                table: "BillingEntries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByEndpoint",
                table: "AppUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedInOperationId",
                table: "AppUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByEndpoint",
                table: "AppUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "AppUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedInOperationId",
                table: "AppUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByEndpoint",
                table: "AppUserRoles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedInOperationId",
                table: "AppUserRoles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByEndpoint",
                table: "AppUserRoles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "AppUserRoles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedInOperationId",
                table: "AppUserRoles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByEndpoint",
                table: "AppRoles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedInOperationId",
                table: "AppRoles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByEndpoint",
                table: "AppRoles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "AppRoles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedInOperationId",
                table: "AppRoles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    EntityName = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataJson = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_AuditEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OperationTraces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Endpoint = table.Column<string>(type: "text", nullable: false),
                    Screen = table.Column<string>(type: "text", nullable: true),
                    Action = table.Column<string>(type: "text", nullable: true),
                    Outcome = table.Column<string>(type: "text", nullable: false),
                    StatusCode = table.Column<int>(type: "integer", nullable: false),
                    DurationMilliseconds = table.Column<long>(type: "bigint", nullable: false),
                    ErrorType = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_OperationTraces", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_OperationId",
                table: "AuditEvents",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationTraces_OperationId",
                table: "OperationTraces",
                column: "OperationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEvents");

            migrationBuilder.DropTable(
                name: "OperationTraces");

            migrationBuilder.DropColumn(
                name: "CreatedByEndpoint",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "CreatedInOperationId",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "UpdatedByEndpoint",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "UpdatedInOperationId",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "CreatedByEndpoint",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "CreatedInOperationId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "UpdatedByEndpoint",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "UpdatedInOperationId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "CreatedByEndpoint",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "CreatedInOperationId",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "UpdatedByEndpoint",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "UpdatedInOperationId",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "CreatedByEndpoint",
                table: "ReminderRecords");

            migrationBuilder.DropColumn(
                name: "CreatedInOperationId",
                table: "ReminderRecords");

            migrationBuilder.DropColumn(
                name: "UpdatedByEndpoint",
                table: "ReminderRecords");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "ReminderRecords");

            migrationBuilder.DropColumn(
                name: "UpdatedInOperationId",
                table: "ReminderRecords");

            migrationBuilder.DropColumn(
                name: "CreatedByEndpoint",
                table: "ReferenceValues");

            migrationBuilder.DropColumn(
                name: "CreatedInOperationId",
                table: "ReferenceValues");

            migrationBuilder.DropColumn(
                name: "UpdatedByEndpoint",
                table: "ReferenceValues");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "ReferenceValues");

            migrationBuilder.DropColumn(
                name: "UpdatedInOperationId",
                table: "ReferenceValues");

            migrationBuilder.DropColumn(
                name: "CreatedByEndpoint",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "CreatedInOperationId",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "UpdatedByEndpoint",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "UpdatedInOperationId",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "CreatedByEndpoint",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CreatedInOperationId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "UpdatedByEndpoint",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "UpdatedInOperationId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CreatedByEndpoint",
                table: "ProgressBillings");

            migrationBuilder.DropColumn(
                name: "CreatedInOperationId",
                table: "ProgressBillings");

            migrationBuilder.DropColumn(
                name: "UpdatedByEndpoint",
                table: "ProgressBillings");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "ProgressBillings");

            migrationBuilder.DropColumn(
                name: "UpdatedInOperationId",
                table: "ProgressBillings");

            migrationBuilder.DropColumn(
                name: "CreatedByEndpoint",
                table: "PaymentInvoiceAllocations");

            migrationBuilder.DropColumn(
                name: "CreatedInOperationId",
                table: "PaymentInvoiceAllocations");

            migrationBuilder.DropColumn(
                name: "UpdatedByEndpoint",
                table: "PaymentInvoiceAllocations");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "PaymentInvoiceAllocations");

            migrationBuilder.DropColumn(
                name: "UpdatedInOperationId",
                table: "PaymentInvoiceAllocations");

            migrationBuilder.DropColumn(
                name: "CreatedByEndpoint",
                table: "MaterialStocks");

            migrationBuilder.DropColumn(
                name: "CreatedInOperationId",
                table: "MaterialStocks");

            migrationBuilder.DropColumn(
                name: "UpdatedByEndpoint",
                table: "MaterialStocks");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "MaterialStocks");

            migrationBuilder.DropColumn(
                name: "UpdatedInOperationId",
                table: "MaterialStocks");

            migrationBuilder.DropColumn(
                name: "CreatedByEndpoint",
                table: "ExecutionGuards");

            migrationBuilder.DropColumn(
                name: "CreatedInOperationId",
                table: "ExecutionGuards");

            migrationBuilder.DropColumn(
                name: "UpdatedByEndpoint",
                table: "ExecutionGuards");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "ExecutionGuards");

            migrationBuilder.DropColumn(
                name: "UpdatedInOperationId",
                table: "ExecutionGuards");

            migrationBuilder.DropColumn(
                name: "CreatedByEndpoint",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CreatedInOperationId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "UpdatedByEndpoint",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "UpdatedInOperationId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CreatedByEndpoint",
                table: "CustomerPayments");

            migrationBuilder.DropColumn(
                name: "CreatedInOperationId",
                table: "CustomerPayments");

            migrationBuilder.DropColumn(
                name: "UpdatedByEndpoint",
                table: "CustomerPayments");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "CustomerPayments");

            migrationBuilder.DropColumn(
                name: "UpdatedInOperationId",
                table: "CustomerPayments");

            migrationBuilder.DropColumn(
                name: "CreatedByEndpoint",
                table: "CustomerLedgerEntries");

            migrationBuilder.DropColumn(
                name: "CreatedInOperationId",
                table: "CustomerLedgerEntries");

            migrationBuilder.DropColumn(
                name: "UpdatedByEndpoint",
                table: "CustomerLedgerEntries");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "CustomerLedgerEntries");

            migrationBuilder.DropColumn(
                name: "UpdatedInOperationId",
                table: "CustomerLedgerEntries");

            migrationBuilder.DropColumn(
                name: "CreatedByEndpoint",
                table: "CustomerConversions");

            migrationBuilder.DropColumn(
                name: "CreatedInOperationId",
                table: "CustomerConversions");

            migrationBuilder.DropColumn(
                name: "UpdatedByEndpoint",
                table: "CustomerConversions");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "CustomerConversions");

            migrationBuilder.DropColumn(
                name: "UpdatedInOperationId",
                table: "CustomerConversions");

            migrationBuilder.DropColumn(
                name: "CreatedByEndpoint",
                table: "CandidateCustomers");

            migrationBuilder.DropColumn(
                name: "CreatedInOperationId",
                table: "CandidateCustomers");

            migrationBuilder.DropColumn(
                name: "UpdatedByEndpoint",
                table: "CandidateCustomers");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "CandidateCustomers");

            migrationBuilder.DropColumn(
                name: "UpdatedInOperationId",
                table: "CandidateCustomers");

            migrationBuilder.DropColumn(
                name: "CreatedByEndpoint",
                table: "BillingEntries");

            migrationBuilder.DropColumn(
                name: "CreatedInOperationId",
                table: "BillingEntries");

            migrationBuilder.DropColumn(
                name: "UpdatedByEndpoint",
                table: "BillingEntries");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "BillingEntries");

            migrationBuilder.DropColumn(
                name: "UpdatedInOperationId",
                table: "BillingEntries");

            migrationBuilder.DropColumn(
                name: "CreatedByEndpoint",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "CreatedInOperationId",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "UpdatedByEndpoint",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "UpdatedInOperationId",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "CreatedByEndpoint",
                table: "AppUserRoles");

            migrationBuilder.DropColumn(
                name: "CreatedInOperationId",
                table: "AppUserRoles");

            migrationBuilder.DropColumn(
                name: "UpdatedByEndpoint",
                table: "AppUserRoles");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "AppUserRoles");

            migrationBuilder.DropColumn(
                name: "UpdatedInOperationId",
                table: "AppUserRoles");

            migrationBuilder.DropColumn(
                name: "CreatedByEndpoint",
                table: "AppRoles");

            migrationBuilder.DropColumn(
                name: "CreatedInOperationId",
                table: "AppRoles");

            migrationBuilder.DropColumn(
                name: "UpdatedByEndpoint",
                table: "AppRoles");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "AppRoles");

            migrationBuilder.DropColumn(
                name: "UpdatedInOperationId",
                table: "AppRoles");
        }
    }
}
