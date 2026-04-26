using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Antital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUnverifiedOtpFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UnverifiedOtpCreatedAtUtc",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UnverifiedOtpExpiresAtUtc",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnverifiedOtpHash",
                table: "Users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnverifiedOtpCreatedAtUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UnverifiedOtpExpiresAtUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UnverifiedOtpHash",
                table: "Users");
        }
    }
}
