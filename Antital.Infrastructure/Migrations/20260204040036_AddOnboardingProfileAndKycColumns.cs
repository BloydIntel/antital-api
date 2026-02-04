using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Antital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOnboardingProfileAndKycColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IncomeVerificationDocumentTypes",
                table: "UserKycs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AdequateLiquidityForLosses",
                table: "UserInvestmentProfiles",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AwareOfLimitedLiquidityHni",
                table: "UserInvestmentProfiles",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AwareOfLimitedLiquiditySophisticated",
                table: "UserInvestmentProfiles",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ConfirmCrowdfundingAssessment",
                table: "UserInvestmentProfiles",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ConfirmSecHniCriteria",
                table: "UserInvestmentProfiles",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ConfirmSecSophisticatedCriteria",
                table: "UserInvestmentProfiles",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "InvestedInPrivateMarketsBefore",
                table: "UserInvestmentProfiles",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvestmentTypes",
                table: "UserInvestmentProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NetAssetsExceed100m",
                table: "UserInvestmentProfiles",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NetInvestmentAssetsRange",
                table: "UserInvestmentProfiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceOfWealth",
                table: "UserInvestmentProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceOfWealthOther",
                table: "UserInvestmentProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YearsActivelyInvesting",
                table: "UserInvestmentProfiles",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IncomeVerificationDocumentTypes",
                table: "UserKycs");

            migrationBuilder.DropColumn(
                name: "AdequateLiquidityForLosses",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "AwareOfLimitedLiquidityHni",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "AwareOfLimitedLiquiditySophisticated",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "ConfirmCrowdfundingAssessment",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "ConfirmSecHniCriteria",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "ConfirmSecSophisticatedCriteria",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "InvestedInPrivateMarketsBefore",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "InvestmentTypes",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "NetAssetsExceed100m",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "NetInvestmentAssetsRange",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "SourceOfWealth",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "SourceOfWealthOther",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "YearsActivelyInvesting",
                table: "UserInvestmentProfiles");
        }
    }
}
