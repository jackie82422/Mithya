using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MockServer.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mock_endpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Protocol = table.Column<int>(type: "integer", nullable: false),
                    Path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    HttpMethod = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DefaultResponse = table.Column<string>(type: "text", nullable: true),
                    DefaultStatusCode = table.Column<int>(type: "integer", nullable: true),
                    ProtocolSettings = table.Column<string>(type: "jsonb", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mock_endpoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "mock_request_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EndpointId = table.Column<Guid>(type: "uuid", nullable: true),
                    RuleId = table.Column<Guid>(type: "uuid", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    QueryString = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Headers = table.Column<string>(type: "jsonb", nullable: true),
                    Body = table.Column<string>(type: "text", nullable: true),
                    ResponseStatusCode = table.Column<int>(type: "integer", nullable: false),
                    ResponseBody = table.Column<string>(type: "text", nullable: true),
                    ResponseTimeMs = table.Column<int>(type: "integer", nullable: false),
                    IsMatched = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mock_request_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "mock_rules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EndpointId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    MatchConditions = table.Column<string>(type: "jsonb", nullable: false),
                    ResponseStatusCode = table.Column<int>(type: "integer", nullable: false, defaultValue: 200),
                    ResponseBody = table.Column<string>(type: "text", nullable: false),
                    ResponseHeaders = table.Column<string>(type: "jsonb", nullable: true),
                    DelayMs = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mock_rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mock_rules_mock_endpoints_EndpointId",
                        column: x => x.EndpointId,
                        principalTable: "mock_endpoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_endpoint_path_method_active",
                table: "mock_endpoints",
                columns: new[] { "Path", "HttpMethod", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "idx_log_endpoint",
                table: "mock_request_logs",
                column: "EndpointId");

            migrationBuilder.CreateIndex(
                name: "idx_log_timestamp",
                table: "mock_request_logs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "idx_rule_endpoint_priority",
                table: "mock_rules",
                columns: new[] { "EndpointId", "Priority" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mock_request_logs");

            migrationBuilder.DropTable(
                name: "mock_rules");

            migrationBuilder.DropTable(
                name: "mock_endpoints");
        }
    }
}
