using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MockServer.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEndpointPathMethodUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "uq_endpoint_path_method",
                table: "mock_endpoints",
                columns: new[] { "Path", "HttpMethod" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_endpoint_path_method",
                table: "mock_endpoints");
        }
    }
}
