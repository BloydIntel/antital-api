using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Antital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFundRaiserCheckpoint3PaymentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FundRaiserApplicationFeePaid",
                table: "UserInvestmentProfiles",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FundRaiserPaymentMethod",
                table: "UserInvestmentProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FundRaiserPaymentReference",
                table: "UserInvestmentProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FundRaiserPaymentStatus",
                table: "UserInvestmentProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FundRaiserApplicationFeePaid",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "FundRaiserPaymentMethod",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "FundRaiserPaymentReference",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "FundRaiserPaymentStatus",
                table: "UserInvestmentProfiles");
        }
    }
}
