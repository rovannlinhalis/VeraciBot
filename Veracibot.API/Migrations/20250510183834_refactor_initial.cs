using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Veracibot.API.Migrations
{
    /// <inheritdoc />
    public partial class refactor_initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "author_balance",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    author_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    tweet_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    value = table.Column<double>(type: "float", nullable: false),
                    previous_balance = table.Column<double>(type: "float", nullable: false),
                    current_balance = table.Column<double>(type: "float", nullable: false),
                    type = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_author_balance", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tweet_authors",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    user_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tweet_authors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tweets",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    text = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    author_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    thread_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    original_author_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    original_text = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    tweet_veracity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tweets", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_author_balance_author_id",
                table: "author_balance",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_author_balance_type",
                table: "author_balance",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "ix_tweet_authors_user_name",
                table: "tweet_authors",
                column: "user_name");

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
                name: "author_balance");

            migrationBuilder.DropTable(
                name: "tweet_authors");

            migrationBuilder.DropTable(
                name: "tweets");
        }
    }
}
