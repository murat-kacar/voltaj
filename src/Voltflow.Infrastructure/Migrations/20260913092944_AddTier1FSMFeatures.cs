using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voltflow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTier1FSMFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSafetyChecklistCompleted",
                table: "WorkOrders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "AppliedDepositAmount",
                table: "SalesInvoices",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "SalesInvoices",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsChangeOrder",
                table: "Quotes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentWorkOrderId",
                table: "Quotes",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSafetyChecklistCompleted",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "AppliedDepositAmount",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "IsChangeOrder",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "ParentWorkOrderId",
                table: "Quotes");
        }
    }
}
