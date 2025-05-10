using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Veracibot.API.Migrations
{
    /// <inheritdoc />
    public partial class initial_tweets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tweets",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    text = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    author_id = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    thread_id = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    original_author_id = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    original_text = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    result = table.Column<int>(type: "INTEGER", nullable: false),
                    date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    tweet_veracity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tweets", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tweets_author_id",
                table: "tweets",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_tweets_original_author_id",
                table: "tweets",
                column: "original_author_id");

            migrationBuilder.CreateIndex(
                name: "ix_tweets_thread_id",
                table: "tweets",
                column: "thread_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tweets");
        }
    }
}
