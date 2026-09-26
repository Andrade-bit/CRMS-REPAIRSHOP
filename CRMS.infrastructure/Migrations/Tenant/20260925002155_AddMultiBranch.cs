using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMS.infrastructure.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddMultiBranch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Staff",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "ServiceTickets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CloudId",
                table: "Branches",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "Branches",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SyncedAt",
                table: "Branches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Staff_BranchId",
                table: "Staff",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTickets_BranchId",
                table: "ServiceTickets",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceTickets_Branches_BranchId",
                table: "ServiceTickets",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "BranchId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Staff_Branches_BranchId",
                table: "Staff",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "BranchId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceTickets_Branches_BranchId",
                table: "ServiceTickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Staff_Branches_BranchId",
                table: "Staff");

            migrationBuilder.DropIndex(
                name: "IX_Staff_BranchId",
                table: "Staff");

            migrationBuilder.DropIndex(
                name: "IX_ServiceTickets_BranchId",
                table: "ServiceTickets");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Staff");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "ServiceTickets");

            migrationBuilder.DropColumn(
                name: "CloudId",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "SyncedAt",
                table: "Branches");
        }
    }
}
