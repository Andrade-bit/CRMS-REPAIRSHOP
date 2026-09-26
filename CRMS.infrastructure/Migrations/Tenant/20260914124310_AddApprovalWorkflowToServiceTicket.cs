using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMS.infrastructure.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddApprovalWorkflowToServiceTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovalDecisionAt",
                table: "ServiceTickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalMethod",
                table: "ServiceTickets",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                table: "ServiceTickets",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "ApprovedAmount",
                table: "ServiceTickets",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiagnosisFindings",
                table: "ServiceTickets",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovalDecisionAt",
                table: "ServiceTickets");

            migrationBuilder.DropColumn(
                name: "ApprovalMethod",
                table: "ServiceTickets");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "ServiceTickets");

            migrationBuilder.DropColumn(
                name: "ApprovedAmount",
                table: "ServiceTickets");

            migrationBuilder.DropColumn(
                name: "DiagnosisFindings",
                table: "ServiceTickets");
        }
    }
}
