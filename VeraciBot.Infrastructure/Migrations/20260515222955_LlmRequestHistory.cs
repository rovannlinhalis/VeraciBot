using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeraciBot.App.Migrations
{
    /// <inheritdoc />
    public partial class LlmRequestHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "llm_request_history",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProcessedMentionTweetId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RequestedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Model = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PromptTokens = table.Column<int>(type: "int", nullable: true),
                    CompletionTokens = table.Column<int>(type: "int", nullable: true),
                    TotalTokens = table.Column<int>(type: "int", nullable: true),
                    LlmResult = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_llm_request_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_llm_request_history_processed_mentions_ProcessedMentionTweetId",
                        column: x => x.ProcessedMentionTweetId,
                        principalTable: "processed_mentions",
                        principalColumn: "TweetId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_llm_request_history_Model",
                table: "llm_request_history",
                column: "Model");

            migrationBuilder.CreateIndex(
                name: "IX_llm_request_history_ProcessedMentionTweetId",
                table: "llm_request_history",
                column: "ProcessedMentionTweetId");

            migrationBuilder.CreateIndex(
                name: "IX_llm_request_history_RequestedAtUtc",
                table: "llm_request_history",
                column: "RequestedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "llm_request_history");
        }
    }
}
