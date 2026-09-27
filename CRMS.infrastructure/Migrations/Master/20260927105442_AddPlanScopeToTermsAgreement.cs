using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMS.infrastructure.Migrations.Master
{
    /// <inheritdoc />
    public partial class AddPlanScopeToTermsAgreement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlanScope",
                table: "TermsAgreements",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlanScope",
                table: "TermsAgreements");
        }
    }
}
