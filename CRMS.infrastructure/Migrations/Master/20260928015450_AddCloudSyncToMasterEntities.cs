using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMS.infrastructure.Migrations.Master
{
    /// <inheritdoc />
    public partial class AddCloudSyncToMasterEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CloudId",
                table: "TermsAgreements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "TermsAgreements",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SyncedAt",
                table: "TermsAgreements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CloudId",
                table: "Superadmins",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "Superadmins",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SyncedAt",
                table: "Superadmins",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CloudId",
                table: "Companies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "Companies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SyncedAt",
                table: "Companies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SyncedAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CloudId",
                table: "Admins",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "Admins",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SyncedAt",
                table: "Admins",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CloudId",
                table: "TermsAgreements");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "TermsAgreements");

            migrationBuilder.DropColumn(
                name: "SyncedAt",
                table: "TermsAgreements");

            migrationBuilder.DropColumn(
                name: "CloudId",
                table: "Superadmins");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "Superadmins");

            migrationBuilder.DropColumn(
                name: "SyncedAt",
                table: "Superadmins");

            migrationBuilder.DropColumn(
                name: "CloudId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "SyncedAt",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SyncedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CloudId",
                table: "Admins");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "Admins");

            migrationBuilder.DropColumn(
                name: "SyncedAt",
                table: "Admins");
        }
    }
}
