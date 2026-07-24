using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Antital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFundraiserNotificationPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FundraiserNotificationPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    EmailCampaignUpdates = table.Column<bool>(type: "boolean", nullable: false),
                    EmailNewInvestments = table.Column<bool>(type: "boolean", nullable: false),
                    EmailSecurityAlerts = table.Column<bool>(type: "boolean", nullable: false),
                    EmailMuted = table.Column<bool>(type: "boolean", nullable: false),
                    InAppRealTimeActivity = table.Column<bool>(type: "boolean", nullable: false),
                    InAppChatMessages = table.Column<bool>(type: "boolean", nullable: false),
                    InAppSystemStatus = table.Column<bool>(type: "boolean", nullable: false),
                    InAppMuted = table.Column<bool>(type: "boolean", nullable: false),
                    MarketingProductNews = table.Column<bool>(type: "boolean", nullable: false),
                    MarketingInvestorTips = table.Column<bool>(type: "boolean", nullable: false),
                    MarketingPartner = table.Column<bool>(type: "boolean", nullable: false),
                    MarketingMuted = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_FundraiserNotificationPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FundraiserNotificationPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FundraiserNotificationPreferences_UserId",
                table: "FundraiserNotificationPreferences",
                column: "UserId",
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FundraiserNotificationPreferences");
        }
    }
}
