using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeraciBot.App.Migrations
{
    /// <inheritdoc />
    public partial class TwitterAuthorizationHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TwitterUsername",
                table: "AspNetUsers",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "authorized_twitter_users",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeauthorizationDate",
                table: "authorized_twitter_users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "authorized_twitter_users",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "authorized_twitter_users",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "authorized_twitter_users",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "authorized_twitter_user_history",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AuthorId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreviousStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApplicationUserId = table.Column<long>(type: "bigint", nullable: true),
                    ChangedByApplicationUserId = table.Column<long>(type: "bigint", nullable: true),
                    ChangedByAuthorId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_authorized_twitter_user_history", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_authorized_twitter_users_Status",
                table: "authorized_twitter_users",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_authorized_twitter_user_history_AuthorId",
                table: "authorized_twitter_user_history",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_authorized_twitter_user_history_ChangedAtUtc",
                table: "authorized_twitter_user_history",
                column: "ChangedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "authorized_twitter_user_history");

            migrationBuilder.DropIndex(
                name: "IX_authorized_twitter_users_Status",
                table: "authorized_twitter_users");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "authorized_twitter_users",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "TwitterUsername",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "authorized_twitter_users");

            migrationBuilder.DropColumn(
                name: "DeauthorizationDate",
                table: "authorized_twitter_users");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "authorized_twitter_users");
        }
    }
}
