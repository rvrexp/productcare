using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductCare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyUsageQuotas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UsageCounters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Operation = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UsageDateUtc = table.Column<DateOnly>(type: "date", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageCounters", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UsageCounters_TenantId_UserId_Operation_UsageDateUtc",
                table: "UsageCounters",
                columns: new[] { "TenantId", "UserId", "Operation", "UsageDateUtc" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsageCounters");
        }
    }
}
