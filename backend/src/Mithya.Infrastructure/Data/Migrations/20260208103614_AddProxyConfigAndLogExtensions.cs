using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mithya.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProxyConfigAndLogExtensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsProxied",
                table: "mock_request_logs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProxyTargetUrl",
                table: "mock_request_logs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "proxy_configs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EndpointId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetBaseUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsRecording = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ForwardHeaders = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    AdditionalHeaders = table.Column<string>(type: "jsonb", nullable: true),
                    TimeoutMs = table.Column<int>(type: "integer", nullable: false, defaultValue: 10000),
                    StripPathPrefix = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proxy_configs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_proxy_configs_mock_endpoints_EndpointId",
                        column: x => x.EndpointId,
                        principalTable: "mock_endpoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_proxy_configs_EndpointId",
                table: "proxy_configs",
                column: "EndpointId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "proxy_configs");

            migrationBuilder.DropColumn(
                name: "IsProxied",
                table: "mock_request_logs");

            migrationBuilder.DropColumn(
                name: "ProxyTargetUrl",
                table: "mock_request_logs");
        }
    }
}
