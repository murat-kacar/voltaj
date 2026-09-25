using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voltflow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DecidedAt",
                table: "Quotes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IssuedAt",
                table: "Quotes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Quotes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ValidUntil",
                table: "Quotes",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "QuoteItem",
                type: "text",
                nullable: false,
                defaultValue: "Service");

            migrationBuilder.AddColumn<int>(
                name: "LineNumber",
                table: "QuoteItem",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "QuoteItem",
                type: "text",
                nullable: false,
                defaultValue: "adet");

            migrationBuilder.AddColumn<decimal>(
                name: "VatRate",
                table: "QuoteItem",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 20m);

            // the lines that were there before get their position on the document
            migrationBuilder.Sql(
                """
                UPDATE "QuoteItem" AS item
                SET "LineNumber" = numbered.position
                FROM (
                    SELECT "Id", ROW_NUMBER() OVER (PARTITION BY "QuoteId" ORDER BY "Id") AS position
                    FROM "QuoteItem"
                ) AS numbered
                WHERE item."Id" = numbered."Id";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_CustomerId",
                table: "Quotes",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_State_ValidUntil",
                table: "Quotes",
                columns: new[] { "State", "ValidUntil" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Quotes_CustomerId",
                table: "Quotes");

            migrationBuilder.DropIndex(
                name: "IX_Quotes_State_ValidUntil",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "DecidedAt",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "IssuedAt",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "ValidUntil",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "QuoteItem");

            migrationBuilder.DropColumn(
                name: "LineNumber",
                table: "QuoteItem");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "QuoteItem");

            migrationBuilder.DropColumn(
                name: "VatRate",
                table: "QuoteItem");
        }
    }
}
