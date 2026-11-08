using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KinTraffic.Api.Migrations
{
    /// <inheritdoc />
    public partial class AlignFaultSqlFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Faults",
                newName: "DetectedAt");

            migrationBuilder.AddColumn<string>(
                name: "FaultType",
                table: "Faults",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FaultType",
                table: "Faults");

            migrationBuilder.RenameColumn(
                name: "DetectedAt",
                table: "Faults",
                newName: "CreatedAt");
        }
    }
}
