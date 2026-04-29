using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Antital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCorporateOnboardingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BoardResolutionDocumentPathOrKey",
                table: "UserKycs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IncorporationCertificateDocumentPathOrKey",
                table: "UserKycs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QiiLicenseEvidenceDocumentPathOrKey",
                table: "UserKycs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecentStatusReportDocumentPathOrKey",
                table: "UserKycs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessAddress",
                table: "UserInvestmentProfiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyEmail",
                table: "UserInvestmentProfiles",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyLegalName",
                table: "UserInvestmentProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyLoginEmail",
                table: "UserInvestmentProfiles",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyPhone",
                table: "UserInvestmentProfiles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyWebsite",
                table: "UserInvestmentProfiles",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ConfirmsSecNigeriaQiiCriteria",
                table: "UserInvestmentProfiles",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfRegistration",
                table: "UserInvestmentProfiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasApprovedAlternativeInvestmentMandate",
                table: "UserInvestmentProfiles",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasBoardResolutionOrInternalMandate",
                table: "UserInvestmentProfiles",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasFinancialCapacityToWithstandLoss",
                table: "UserInvestmentProfiles",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasQualifiedInvestmentProfessionalsAccess",
                table: "UserInvestmentProfiles",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasValidQiiRegistrationOrLicense",
                table: "UserInvestmentProfiles",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OciNetAssetValueRange",
                table: "UserInvestmentProfiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QiiInstitutionTypes",
                table: "UserInvestmentProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QiiOtherInstitutionType",
                table: "UserInvestmentProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegisteredAddress",
                table: "UserInvestmentProfiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationNumber",
                table: "UserInvestmentProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationType",
                table: "UserInvestmentProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepresentativeAddress",
                table: "UserInvestmentProfiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepresentativeCountryOfResidence",
                table: "UserInvestmentProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RepresentativeDateOfBirth",
                table: "UserInvestmentProfiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepresentativeEmail",
                table: "UserInvestmentProfiles",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepresentativeFullName",
                table: "UserInvestmentProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepresentativeJobTitle",
                table: "UserInvestmentProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepresentativeNationality",
                table: "UserInvestmentProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepresentativePhoneNumber",
                table: "UserInvestmentProfiles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TradingBrandName",
                table: "UserInvestmentProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UnderstandsCrowdfundingHighRiskLoss",
                table: "UserInvestmentProfiles",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BoardResolutionDocumentPathOrKey",
                table: "UserKycs");

            migrationBuilder.DropColumn(
                name: "IncorporationCertificateDocumentPathOrKey",
                table: "UserKycs");

            migrationBuilder.DropColumn(
                name: "QiiLicenseEvidenceDocumentPathOrKey",
                table: "UserKycs");

            migrationBuilder.DropColumn(
                name: "RecentStatusReportDocumentPathOrKey",
                table: "UserKycs");

            migrationBuilder.DropColumn(
                name: "BusinessAddress",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "CompanyEmail",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "CompanyLegalName",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "CompanyLoginEmail",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "CompanyPhone",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "CompanyWebsite",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "ConfirmsSecNigeriaQiiCriteria",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "DateOfRegistration",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "HasApprovedAlternativeInvestmentMandate",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "HasBoardResolutionOrInternalMandate",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "HasFinancialCapacityToWithstandLoss",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "HasQualifiedInvestmentProfessionalsAccess",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "HasValidQiiRegistrationOrLicense",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "OciNetAssetValueRange",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "QiiInstitutionTypes",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "QiiOtherInstitutionType",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "RegisteredAddress",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "RegistrationNumber",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "RegistrationType",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "RepresentativeAddress",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "RepresentativeCountryOfResidence",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "RepresentativeDateOfBirth",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "RepresentativeEmail",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "RepresentativeFullName",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "RepresentativeJobTitle",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "RepresentativeNationality",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "RepresentativePhoneNumber",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "TradingBrandName",
                table: "UserInvestmentProfiles");

            migrationBuilder.DropColumn(
                name: "UnderstandsCrowdfundingHighRiskLoss",
                table: "UserInvestmentProfiles");
        }
    }
}
