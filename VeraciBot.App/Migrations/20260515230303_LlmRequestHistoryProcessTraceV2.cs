using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeraciBot.App.Migrations
{
    /// <inheritdoc />
    public partial class LlmRequestHistoryProcessTraceV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConsultedNewsLinksJson",
                table: "llm_request_history",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessStepsJson",
                table: "llm_request_history",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsultedNewsLinksJson",
                table: "llm_request_history");

            migrationBuilder.DropColumn(
                name: "ProcessStepsJson",
                table: "llm_request_history");
        }
    }
}
