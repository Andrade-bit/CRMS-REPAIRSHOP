using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMS.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceToMasterCRMS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyID = table.Column<int>(type: "int", nullable: false),
                    DeviceCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsaActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.DeviceId);
                    table.ForeignKey(
                        name: "FK_Devices_Companies_CompanyID",
                        column: x => x.CompanyID,
                        principalTable: "Companies",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Devices_CompanyID_DeviceName",
                table: "Devices",
                columns: new[] { "CompanyID", "DeviceName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Devices");
        }
    }
}
