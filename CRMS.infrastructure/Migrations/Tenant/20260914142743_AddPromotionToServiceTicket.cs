using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMS.infrastructure.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddPromotionToServiceTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PromotionId",
                table: "ServiceTickets",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTickets_PromotionId",
                table: "ServiceTickets",
                column: "PromotionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceTickets_Promotions_PromotionId",
                table: "ServiceTickets",
                column: "PromotionId",
                principalTable: "Promotions",
                principalColumn: "PromotionId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceTickets_Promotions_PromotionId",
                table: "ServiceTickets");

            migrationBuilder.DropIndex(
                name: "IX_ServiceTickets_PromotionId",
                table: "ServiceTickets");

            migrationBuilder.DropColumn(
                name: "PromotionId",
                table: "ServiceTickets");
        }
    }
}
