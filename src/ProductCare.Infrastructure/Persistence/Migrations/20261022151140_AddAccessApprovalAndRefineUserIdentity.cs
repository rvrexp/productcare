using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductCare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessApprovalAndRefineUserIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OwnerSubject",
                table: "Products",
                newName: "OwnerUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Products_OwnerTenantId_OwnerSubject_CreatedUtc",
                table: "Products",
                newName: "IX_Products_OwnerTenantId_OwnerUserId_CreatedUtc");

            migrationBuilder.CreateTable(
                name: "UserAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Plan = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    AccessExpiresUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccessRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Organization = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Purpose = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    RequestedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByTenantId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccessRequests_UserAccounts_UserAccountId",
                        column: x => x.UserAccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessRequests_Status_RequestedUtc",
                table: "AccessRequests",
                columns: new[] { "Status", "RequestedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessRequests_UserAccountId",
                table: "AccessRequests",
                column: "UserAccountId",
                unique: true,
                filter: "[Status] = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_AccessRequests_UserAccountId_RequestedUtc",
                table: "AccessRequests",
                columns: new[] { "UserAccountId", "RequestedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UserAccounts_Status",
                table: "UserAccounts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccounts_TenantId_UserId",
                table: "UserAccounts",
                columns: new[] { "TenantId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessRequests");

            migrationBuilder.DropTable(
                name: "UserAccounts");

            migrationBuilder.RenameColumn(
                name: "OwnerUserId",
                table: "Products",
                newName: "OwnerSubject");

            migrationBuilder.RenameIndex(
                name: "IX_Products_OwnerTenantId_OwnerUserId_CreatedUtc",
                table: "Products",
                newName: "IX_Products_OwnerTenantId_OwnerSubject_CreatedUtc");
        }
    }
}
