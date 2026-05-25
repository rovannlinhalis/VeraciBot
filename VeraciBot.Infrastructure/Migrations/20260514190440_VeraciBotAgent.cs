using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeraciBot.App.Migrations
{
    /// <inheritdoc />
    public partial class VeraciBotAgent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "authorized_twitter_users",
                columns: table => new
                {
                    AuthorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuthorizedById = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuthorizationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Score = table.Column<int>(type: "int", nullable: false),
                    Wins = table.Column<int>(type: "int", nullable: false),
                    Losses = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_authorized_twitter_users", x => x.AuthorId);
                });

            migrationBuilder.CreateTable(
                name: "processed_mentions",
                columns: table => new
                {
                    TweetId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProcessedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Result = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processed_mentions", x => x.TweetId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "authorized_twitter_users");

            migrationBuilder.DropTable(
                name: "processed_mentions");
        }
    }
}
