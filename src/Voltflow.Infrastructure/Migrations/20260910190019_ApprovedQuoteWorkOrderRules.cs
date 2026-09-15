using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voltflow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ApprovedQuoteWorkOrderRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceQuoteId",
                table: "WorkOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_SourceQuoteId",
                table: "WorkOrders",
                column: "SourceQuoteId",
                unique: true,
                filter: "\"SourceQuoteId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_SourceQuoteId",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "SourceQuoteId",
                table: "WorkOrders");
        }
    }
}
