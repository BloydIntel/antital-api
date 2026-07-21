using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Antital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferingOwnerUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OwnerUserId",
                table: "InvestmentOfferings",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentOfferings_OwnerUserId",
                table: "InvestmentOfferings",
                column: "OwnerUserId",
                filter: "\"OwnerUserId\" IS NOT NULL AND \"IsDeleted\" = false");

            migrationBuilder.AddForeignKey(
                name: "FK_InvestmentOfferings_Users_OwnerUserId",
                table: "InvestmentOfferings",
                column: "OwnerUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvestmentOfferings_Users_OwnerUserId",
                table: "InvestmentOfferings");

            migrationBuilder.DropIndex(
                name: "IX_InvestmentOfferings_OwnerUserId",
                table: "InvestmentOfferings");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "InvestmentOfferings");
        }
    }
}
