using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMS.infrastructure.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddArchivedAtToPromotion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "Promotions",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Promotions");
        }
    }
}
