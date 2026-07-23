using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Antital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferingUpdateStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OfferingUpdates_OfferingId",
                table: "OfferingUpdates");

            migrationBuilder.AlterColumn<DateTime>(
                name: "PublishedAt",
                table: "OfferingUpdates",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "OfferingUpdates",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql(
                """
                UPDATE "OfferingUpdates"
                SET "Status" = 1
                WHERE "PublishedAt" IS NOT NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_OfferingUpdates_OfferingId_Status_PublishedAt",
                table: "OfferingUpdates",
                columns: new[] { "OfferingId", "Status", "PublishedAt" },
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OfferingUpdates_OfferingId_Status_PublishedAt",
                table: "OfferingUpdates");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "OfferingUpdates");

            migrationBuilder.AlterColumn<DateTime>(
                name: "PublishedAt",
                table: "OfferingUpdates",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfferingUpdates_OfferingId",
                table: "OfferingUpdates",
                column: "OfferingId");
        }
    }
}
