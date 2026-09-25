using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voltflow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ActorMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "WorkOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "StockMovements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "SalesInvoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "ReminderRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Quotes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Projects",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "ProgressBillings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "PaymentInvoiceAllocations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "MaterialStocks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Customers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "CustomerPayments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "CustomerLedgerEntries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "CustomerConversions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "CandidateCustomers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "BillingEntries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "AppUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "AppUserRoles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "AppRoles",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "ReminderRecords");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "ProgressBillings");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "PaymentInvoiceAllocations");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "MaterialStocks");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "CustomerPayments");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "CustomerLedgerEntries");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "CustomerConversions");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "CandidateCustomers");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "BillingEntries");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "AppUserRoles");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "AppRoles");
        }
    }
}
