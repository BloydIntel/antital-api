using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Antital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferingDocumentManagementFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OfferingDocuments_OfferingId",
                table: "OfferingDocuments");

            migrationBuilder.AlterColumn<string>(
                name: "FileUrl",
                table: "OfferingDocuments",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "OfferingDocuments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CloudinaryPublicId",
                table: "OfferingDocuments",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "OfferingDocuments",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "application/pdf");

            migrationBuilder.AddColumn<long>(
                name: "FileSizeBytes",
                table: "OfferingDocuments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "ReviewStatus",
                table: "OfferingDocuments",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_OfferingDocuments_OfferingId_Category",
                table: "OfferingDocuments",
                columns: new[] { "OfferingId", "Category" },
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OfferingDocuments_OfferingId_Category",
                table: "OfferingDocuments");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "OfferingDocuments");

            migrationBuilder.DropColumn(
                name: "CloudinaryPublicId",
                table: "OfferingDocuments");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "OfferingDocuments");

            migrationBuilder.DropColumn(
                name: "FileSizeBytes",
                table: "OfferingDocuments");

            migrationBuilder.DropColumn(
                name: "ReviewStatus",
                table: "OfferingDocuments");

            migrationBuilder.AlterColumn<string>(
                name: "FileUrl",
                table: "OfferingDocuments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000);

            migrationBuilder.CreateIndex(
                name: "IX_OfferingDocuments_OfferingId",
                table: "OfferingDocuments",
                column: "OfferingId");
        }
    }
}
