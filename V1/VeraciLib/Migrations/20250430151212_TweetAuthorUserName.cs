using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeraciBot.Migrations
{
    /// <inheritdoc />
    public partial class TweetAuthorUserName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "TweetAuthors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "TweetAuthors");
        }
    }
}
