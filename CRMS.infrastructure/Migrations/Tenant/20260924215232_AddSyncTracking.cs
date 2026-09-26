using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMS.infrastructure.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddSyncTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CloudId",
                table: "Staff",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "Staff",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SyncedAt",
                table: "Staff",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CloudId",
                table: "ServiceTickets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "ServiceTickets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SyncedAt",
                table: "ServiceTickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CloudId",
                table: "Promotions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "Promotions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SyncedAt",
                table: "Promotions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CloudId",
                table: "Customers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "Customers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SyncedAt",
                table: "Customers",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CloudId",
                table: "Staff");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "Staff");

            migrationBuilder.DropColumn(
                name: "SyncedAt",
                table: "Staff");

            migrationBuilder.DropColumn(
                name: "CloudId",
                table: "ServiceTickets");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "ServiceTickets");

            migrationBuilder.DropColumn(
                name: "SyncedAt",
                table: "ServiceTickets");

            migrationBuilder.DropColumn(
                name: "CloudId",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "SyncedAt",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "CloudId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "SyncedAt",
                table: "Customers");
        }
    }
}
