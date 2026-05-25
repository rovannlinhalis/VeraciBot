using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeraciBot.App.Migrations
{
    /// <inheritdoc />
    public partial class ApplicationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "application_settings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    value = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_application_settings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "application_settings");
        }
    }
}
