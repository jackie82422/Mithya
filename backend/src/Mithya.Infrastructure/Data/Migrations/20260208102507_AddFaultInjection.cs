using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mithya.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFaultInjection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FaultConfig",
                table: "mock_rules",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FaultType",
                table: "mock_rules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FaultTypeApplied",
                table: "mock_request_logs",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FaultConfig",
                table: "mock_rules");

            migrationBuilder.DropColumn(
                name: "FaultType",
                table: "mock_rules");

            migrationBuilder.DropColumn(
                name: "FaultTypeApplied",
                table: "mock_request_logs");
        }
    }
}
