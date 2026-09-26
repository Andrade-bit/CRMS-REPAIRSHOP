using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMS.infrastructure.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddAmountPaidToServiceTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AmountPaid",
                table: "ServiceTickets",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmountPaid",
                table: "ServiceTickets");
        }
    }
}
