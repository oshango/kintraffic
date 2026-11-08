using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KinTraffic.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialTrafficSignals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrafficSignals",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CycleTime = table.Column<int>(type: "int", nullable: false),
                    QueueLength = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrafficSignals", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "TrafficSignals",
                columns: new[] { "Id", "CycleTime", "Name", "QueueLength", "Status", "UpdatedAt" },
                values: new object[,]
                {
                    { "signal-001", 42, "Main Street & 1st Ave", 12, "normal", new DateTime(2026, 8, 30, 8, 0, 0, 0, DateTimeKind.Utc) },
                    { "signal-002", 58, "Oak Blvd & Market Rd", 24, "warning", new DateTime(2026, 8, 30, 8, 3, 0, 0, DateTimeKind.Utc) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrafficSignals");
        }
    }
}
