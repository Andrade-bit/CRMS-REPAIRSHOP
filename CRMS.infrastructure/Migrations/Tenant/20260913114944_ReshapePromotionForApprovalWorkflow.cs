using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMS.infrastructure.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class ReshapePromotionForApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SentDate",
                table: "Promotions",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "AdminId",
                table: "Promotions",
                newName: "StaffId");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "Promotions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewedByAdminId",
                table: "Promotions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Promotions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Promotions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Promotions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_StaffId",
                table: "Promotions",
                column: "StaffId");

            migrationBuilder.AddForeignKey(
                name: "FK_Promotions_Staff_StaffId",
                table: "Promotions",
                column: "StaffId",
                principalTable: "Staff",
                principalColumn: "StaffId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Promotions_Staff_StaffId",
                table: "Promotions");

            migrationBuilder.DropIndex(
                name: "IX_Promotions_StaffId",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "ReviewedByAdminId",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Promotions");

            migrationBuilder.RenameColumn(
                name: "StaffId",
                table: "Promotions",
                newName: "AdminId");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Promotions",
                newName: "SentDate");
        }
    }
}
