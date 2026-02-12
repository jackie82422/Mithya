using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mithya.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScenariosAndSteps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "scenarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    InitialState = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CurrentState = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scenarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "scenario_steps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScenarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    StateName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EndpointId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchConditions = table.Column<string>(type: "jsonb", nullable: true),
                    ResponseStatusCode = table.Column<int>(type: "integer", nullable: false, defaultValue: 200),
                    ResponseBody = table.Column<string>(type: "text", nullable: false),
                    ResponseHeaders = table.Column<string>(type: "jsonb", nullable: true),
                    IsTemplate = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DelayMs = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    NextState = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 100)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scenario_steps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_scenario_steps_mock_endpoints_EndpointId",
                        column: x => x.EndpointId,
                        principalTable: "mock_endpoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_scenario_steps_scenarios_ScenarioId",
                        column: x => x.ScenarioId,
                        principalTable: "scenarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_scenario_steps_EndpointId",
                table: "scenario_steps",
                column: "EndpointId");

            migrationBuilder.CreateIndex(
                name: "IX_scenario_steps_ScenarioId",
                table: "scenario_steps",
                column: "ScenarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scenario_steps");

            migrationBuilder.DropTable(
                name: "scenarios");
        }
    }
}
