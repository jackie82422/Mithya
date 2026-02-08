using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MockServer.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLogicModeToMockRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LogicMode",
                table: "mock_rules",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "AND");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogicMode",
                table: "mock_rules");
        }
    }
}
