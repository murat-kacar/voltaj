using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voltflow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ApplyFsmStandards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CandidateCustomers");

            migrationBuilder.DropTable(
                name: "CustomerConversions");

            migrationBuilder.AddColumn<Guid>(
                name: "AssetId",
                table: "WorkOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckInTime",
                table: "WorkOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckOutTime",
                table: "WorkOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentWorkOrderId",
                table: "WorkOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PhaseId",
                table: "WorkOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "WorkOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProofOfWorkPhotoUrl",
                table: "WorkOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignatureData",
                table: "WorkOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SiteId",
                table: "WorkOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TargetCompletionDate",
                table: "WorkOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssetId",
                table: "Quotes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DepositPaidAmount",
                table: "Quotes",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RequiredDepositPercentage",
                table: "Quotes",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "SiteId",
                table: "Quotes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Customers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CustomerAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    SerialNumber = table.Column<string>(type: "text", nullable: false),
                    InstallationDate = table.Column<DateOnly>(type: "date", nullable: true),
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
                    table.PrimaryKey("PK_CustomerAssets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerSites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_CustomerSites", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAssets_SerialNumber",
                table: "CustomerAssets",
                column: "SerialNumber",
                unique: true,
                filter: "\"SerialNumber\" <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerAssets");

            migrationBuilder.DropTable(
                name: "CustomerSites");

            migrationBuilder.DropColumn(
                name: "AssetId",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "CheckInTime",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "CheckOutTime",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "ParentWorkOrderId",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "PhaseId",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "ProofOfWorkPhotoUrl",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "SignatureData",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "SiteId",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "TargetCompletionDate",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "AssetId",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "DepositPaidAmount",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "RequiredDepositPercentage",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "SiteId",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Customers");

            migrationBuilder.CreateTable(
                name: "CandidateCustomers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConvertedCustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByEndpoint = table.Column<string>(type: "text", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedInOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: false),
                    TaxNumber = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByEndpoint = table.Column<string>(type: "text", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedInOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateCustomers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerConversions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateCustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByEndpoint = table.Column<string>(type: "text", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedInOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByEndpoint = table.Column<string>(type: "text", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedInOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerConversions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateCustomers_TaxNumber",
                table: "CandidateCustomers",
                column: "TaxNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerConversions_CandidateCustomerId",
                table: "CustomerConversions",
                column: "CandidateCustomerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerConversions_CustomerId",
                table: "CustomerConversions",
                column: "CustomerId",
                unique: true);
        }
    }
}
