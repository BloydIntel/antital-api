using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Antital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyCacVerificationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CacIncorporationDate",
                table: "UserInvestmentProfiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CacVerificationStatus",
                table: "UserInvestmentProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CacVerifiedAt",
                table: "UserInvestmentProfiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CacVerifiedCompanyName",
                table: "UserInvestmentProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CacVerifiedCompanyType",
                table: "UserInvestmentProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CacVerifiedRegistrationNumber",
                table: "UserInvestmentProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CacIncorporationDate",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "CacVerificationStatus",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "CacVerifiedAt",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "CacVerifiedCompanyName",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "CacVerifiedCompanyType",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "CacVerifiedRegistrationNumber",
                table: "UserInvestmentProfiles");
        }
    }
}
