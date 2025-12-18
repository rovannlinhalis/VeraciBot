using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeraciApp.Migrations
{
    /// <inheritdoc />
    public partial class AuthorizedUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
           
            migrationBuilder.CreateTable(
                name: "AuthorizedUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AuthorizedById = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AuthorizationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    NumberOfAuthorizations = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorizedUsers", x => x.Id);
                });

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropTable(
                name: "AuthorizedUsers");

        }
    }
}
