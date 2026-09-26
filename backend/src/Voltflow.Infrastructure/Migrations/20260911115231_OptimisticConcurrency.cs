using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voltflow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OptimisticConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "WorkOrders",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "StockMovements",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "SalesInvoices",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "ReminderRecords",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "ReferenceValues",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Quotes",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Projects",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "ProgressBillings",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "PaymentInvoiceAllocations",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "OutboxMessages",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "OperationTraces",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "MaterialStocks",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "ExecutionGuards",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Customers",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "CustomerPayments",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "CustomerLedgerEntries",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "CustomerConversions",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "CandidateCustomers",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "BillingEntries",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "AuditEvents",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "AppUsers",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "AppUserRoles",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "AppRoles",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ReminderRecords");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ReferenceValues");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ProgressBillings");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "PaymentInvoiceAllocations");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "OperationTraces");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "MaterialStocks");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ExecutionGuards");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "CustomerPayments");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "CustomerLedgerEntries");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "CustomerConversions");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "CandidateCustomers");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "BillingEntries");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "AuditEvents");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "AppUserRoles");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "AppRoles");
        }
    }
}
