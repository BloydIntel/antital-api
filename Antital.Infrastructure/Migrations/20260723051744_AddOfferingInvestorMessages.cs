using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Antital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferingInvestorMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OfferingInvestorMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OfferingId = table.Column<int>(type: "integer", nullable: false),
                    AskerUserId = table.Column<int>(type: "integer", nullable: false),
                    Question = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Reply = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Visibility = table.Column<int>(type: "integer", nullable: false),
                    AskedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RepliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_OfferingInvestorMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfferingInvestorMessages_InvestmentOfferings_OfferingId",
                        column: x => x.OfferingId,
                        principalTable: "InvestmentOfferings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OfferingInvestorMessages_Users_AskerUserId",
                        column: x => x.AskerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OfferingInvestorMessages_AskerUserId",
                table: "OfferingInvestorMessages",
                column: "AskerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferingInvestorMessages_OfferingId_AskedAt",
                table: "OfferingInvestorMessages",
                columns: new[] { "OfferingId", "AskedAt" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_OfferingInvestorMessages_OfferingId_RepliedAt",
                table: "OfferingInvestorMessages",
                columns: new[] { "OfferingId", "RepliedAt" },
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OfferingInvestorMessages");
        }
    }
}
