using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Antital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOnboardingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_EmailVerificationToken",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_RefreshTokenHash",
                table: "Users");

            migrationBuilder.CreateTable(
                name: "UserInvestmentProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    InvestorCategory = table.Column<int>(type: "integer", nullable: false),
                    HighRiskAllocationPast12MonthsPercent = table.Column<decimal>(type: "numeric", nullable: true),
                    HighRiskAllocationNext12MonthsPercent = table.Column<decimal>(type: "numeric", nullable: true),
                    AnnualIncomeRange = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    NetInvestmentAssetsValue = table.Column<decimal>(type: "numeric", nullable: true),
                    CanAffordToLoseWithoutAffectingStability = table.Column<bool>(type: "boolean", nullable: true),
                    UnderstandsCrowdfundingIsHighRisk = table.Column<bool>(type: "boolean", nullable: true),
                    ReadRiskDisclosureAndSecRules = table.Column<bool>(type: "boolean", nullable: true),
                    UnderstandsPastPerformanceNoGuarantee = table.Column<bool>(type: "boolean", nullable: true),
                    AwareOfLimitedLiquidity = table.Column<bool>(type: "boolean", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserInvestmentProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserInvestmentProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserKycs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    IdType = table.Column<int>(type: "integer", nullable: false),
                    Nin = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Bvn = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    GovernmentIdDocumentPathOrKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ProofOfAddressDocumentPathOrKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SelfieVerificationPathOrKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IncomeVerificationPathOrKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    GovernmentIdVerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProofOfAddressVerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SelfieVerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IncomeVerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserKycs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserKycs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserOnboardings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    FlowType = table.Column<int>(type: "integer", nullable: false),
                    CurrentStep = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserOnboardings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserOnboardings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmailVerificationToken",
                table: "Users",
                column: "EmailVerificationToken",
                filter: "\"EmailVerificationToken\" IS NOT NULL AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RefreshTokenHash",
                table: "Users",
                column: "RefreshTokenHash",
                filter: "\"RefreshTokenHash\" IS NOT NULL AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_UserInvestmentProfiles_UserId",
                table: "UserInvestmentProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserKycs_UserId",
                table: "UserKycs",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserOnboardings_UserId",
                table: "UserOnboardings",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserInvestmentProfiles");

            migrationBuilder.DropTable(
                name: "UserKycs");

            migrationBuilder.DropTable(
                name: "UserOnboardings");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_EmailVerificationToken",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_RefreshTokenHash",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmailVerificationToken",
                table: "Users",
                column: "EmailVerificationToken",
                filter: "[EmailVerificationToken] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RefreshTokenHash",
                table: "Users",
                column: "RefreshTokenHash",
                filter: "[RefreshTokenHash] IS NOT NULL AND [IsDeleted] = 0");
        }
    }
}
