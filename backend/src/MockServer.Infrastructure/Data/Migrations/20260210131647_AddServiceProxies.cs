using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MockServer.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceProxies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "service_proxies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TargetBaseUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsRecording = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ForwardHeaders = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    AdditionalHeaders = table.Column<string>(type: "jsonb", nullable: true),
                    TimeoutMs = table.Column<int>(type: "integer", nullable: false, defaultValue: 10000),
                    StripPathPrefix = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FallbackEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_proxies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_service_proxies_ServiceName",
                table: "service_proxies",
                column: "ServiceName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "service_proxies");
        }
    }
}
