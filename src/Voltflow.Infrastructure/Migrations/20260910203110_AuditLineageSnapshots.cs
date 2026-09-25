using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voltflow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AuditLineageSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DataJson",
                table: "AuditEvents",
                newName: "SourceEndpoint");

            migrationBuilder.AddColumn<string>(
                name: "AfterJson",
                table: "AuditEvents",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BeforeJson",
                table: "AuditEvents",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ChangedFieldsJson",
                table: "AuditEvents",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ParentOperationId",
                table: "AuditEvents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceAction",
                table: "AuditEvents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceScreen",
                table: "AuditEvents",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AfterJson",
                table: "AuditEvents");

            migrationBuilder.DropColumn(
                name: "BeforeJson",
                table: "AuditEvents");

            migrationBuilder.DropColumn(
                name: "ChangedFieldsJson",
                table: "AuditEvents");

            migrationBuilder.DropColumn(
                name: "ParentOperationId",
                table: "AuditEvents");

            migrationBuilder.DropColumn(
                name: "SourceAction",
                table: "AuditEvents");

            migrationBuilder.DropColumn(
                name: "SourceScreen",
                table: "AuditEvents");

            migrationBuilder.RenameColumn(
                name: "SourceEndpoint",
                table: "AuditEvents",
                newName: "DataJson");
        }
    }
}
