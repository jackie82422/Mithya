using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mithya.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEndpointGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "endpoint_groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_endpoint_groups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "endpoint_group_mappings",
                columns: table => new
                {
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    EndpointId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_endpoint_group_mappings", x => new { x.GroupId, x.EndpointId });
                    table.ForeignKey(
                        name: "FK_endpoint_group_mappings_endpoint_groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "endpoint_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_endpoint_group_mappings_mock_endpoints_EndpointId",
                        column: x => x.EndpointId,
                        principalTable: "mock_endpoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_endpoint_group_mappings_EndpointId",
                table: "endpoint_group_mappings",
                column: "EndpointId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "endpoint_group_mappings");

            migrationBuilder.DropTable(
                name: "endpoint_groups");
        }
    }
}
