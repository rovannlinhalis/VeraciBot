using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeraciBot.Migrations
{
    /// <inheritdoc />
    public partial class ApplicationUserAuthorId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthorId",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthorId",
                table: "AspNetUsers");
        }
    }
}
