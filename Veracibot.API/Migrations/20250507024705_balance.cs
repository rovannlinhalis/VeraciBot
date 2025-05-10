using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Veracibot.API.Migrations
{
    /// <inheritdoc />
    public partial class balance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "thread_id",
                table: "tweets",
                type: "TEXT",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "text",
                table: "tweets",
                type: "TEXT",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "original_text",
                table: "tweets",
                type: "TEXT",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "original_author_id",
                table: "tweets",
                type: "TEXT",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "author_id",
                table: "tweets",
                type: "TEXT",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 255);

            migrationBuilder.CreateTable(
                name: "author_balance",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    author_id = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    tweet_id = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    value = table.Column<double>(type: "REAL", nullable: false),
                    previous_balance = table.Column<double>(type: "REAL", nullable: false),
                    current_balance = table.Column<double>(type: "REAL", nullable: false),
                    type = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_author_balance", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tweet_authors",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    user_name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tweet_authors", x => x.id);
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "author_balance");

            migrationBuilder.DropTable(
                name: "tweet_authors");

            migrationBuilder.AlterColumn<string>(
                name: "thread_id",
                table: "tweets",
                type: "TEXT",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "text",
                table: "tweets",
                type: "TEXT",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "original_text",
                table: "tweets",
                type: "TEXT",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "original_author_id",
                table: "tweets",
                type: "TEXT",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "author_id",
                table: "tweets",
                type: "TEXT",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 255,
                oldNullable: true);
        }
    }
}
