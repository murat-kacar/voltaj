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
                name: "SourceServiceId",
                table: "WorkOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_SourceServiceId",
                table: "WorkOrders",
                column: "SourceServiceId",
                unique: true,
                filter: "\"SourceServiceId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_SourceServiceId",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "SourceServiceId",
                table: "WorkOrders");
        }
    }
}
