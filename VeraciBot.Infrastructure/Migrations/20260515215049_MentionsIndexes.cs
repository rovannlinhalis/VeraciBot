using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeraciBot.App.Migrations
{
    /// <inheritdoc />
    public partial class MentionsIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Result",
                table: "processed_mentions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuthorId",
                table: "processed_mentions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Text",
                table: "processed_mentions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_processed_mentions_AuthorId",
                table: "processed_mentions",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_processed_mentions_ProcessedAtUtc",
                table: "processed_mentions",
                column: "ProcessedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_processed_mentions_Result",
                table: "processed_mentions",
                column: "Result");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_processed_mentions_AuthorId",
                table: "processed_mentions");

            migrationBuilder.DropIndex(
                name: "IX_processed_mentions_ProcessedAtUtc",
                table: "processed_mentions");

            migrationBuilder.DropIndex(
                name: "IX_processed_mentions_Result",
                table: "processed_mentions");

            migrationBuilder.DropColumn(
                name: "AuthorId",
                table: "processed_mentions");

            migrationBuilder.DropColumn(
                name: "Text",
                table: "processed_mentions");

            migrationBuilder.AlterColumn<string>(
                name: "Result",
                table: "processed_mentions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);
        }
    }
}
