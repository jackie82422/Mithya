using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mithya.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTemplateFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsResponseHeadersTemplate",
                table: "mock_rules",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTemplate",
                table: "mock_rules",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsResponseHeadersTemplate",
                table: "mock_rules");

            migrationBuilder.DropColumn(
                name: "IsTemplate",
                table: "mock_rules");
        }
    }
}
