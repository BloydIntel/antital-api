using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Antital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFundRaiserCheckpoint2Fields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BusinessDescription",
                table: "UserInvestmentProfiles",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessSector",
                table: "UserInvestmentProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessSize",
                table: "UserInvestmentProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FounderAndTeamIntroductionDocumentPathOrKey",
                table: "UserInvestmentProfiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FundingTarget",
                table: "UserInvestmentProfiles",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FundraisingDeckDocumentPathOrKey",
                table: "UserInvestmentProfiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstrumentType",
                table: "UserInvestmentProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvestmentMemoDocumentPathOrKey",
                table: "UserInvestmentProfiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvestmentRound",
                table: "UserInvestmentProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductDemoDocumentPathOrKey",
                table: "UserInvestmentProfiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TermsOfOfferingDocumentPathOrKey",
                table: "UserInvestmentProfiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessDescription",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "BusinessSector",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "BusinessSize",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "FounderAndTeamIntroductionDocumentPathOrKey",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "FundingTarget",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "FundraisingDeckDocumentPathOrKey",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "InstrumentType",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "InvestmentMemoDocumentPathOrKey",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "InvestmentRound",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "ProductDemoDocumentPathOrKey",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "TermsOfOfferingDocumentPathOrKey",
                table: "UserInvestmentProfiles");
        }
    }
}
