using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeraciBot.App.Migrations
{
    /// <inheritdoc />
    public partial class InviteAccounting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InviteCredits",
                table: "authorized_twitter_users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InvitesAccepted",
                table: "authorized_twitter_users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InvitesSent",
                table: "authorized_twitter_users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "twitter_invite_credit_transactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AuthorId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Username = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Delta = table.Column<int>(type: "int", nullable: false),
                    BalanceAfter = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangedByApplicationUserId = table.Column<long>(type: "bigint", nullable: true),
                    ChangedByAuthorId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_twitter_invite_credit_transactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "twitter_invites",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InviterAuthorId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    InviterUsername = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    InviteeAuthorId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    InviteeUsername = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    InviteeName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcceptedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RefusedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SourceTweetId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedByApplicationUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_twitter_invites", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_twitter_invite_credit_transactions_AuthorId",
                table: "twitter_invite_credit_transactions",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_twitter_invite_credit_transactions_CreatedAtUtc",
                table: "twitter_invite_credit_transactions",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_twitter_invites_CreatedAtUtc",
                table: "twitter_invites",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_twitter_invites_InviteeAuthorId",
                table: "twitter_invites",
                column: "InviteeAuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_twitter_invites_InviterAuthorId",
                table: "twitter_invites",
                column: "InviterAuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_twitter_invites_Status",
                table: "twitter_invites",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "twitter_invite_credit_transactions");

            migrationBuilder.DropTable(
                name: "twitter_invites");

            migrationBuilder.DropColumn(
                name: "InviteCredits",
                table: "authorized_twitter_users");

            migrationBuilder.DropColumn(
                name: "InvitesAccepted",
                table: "authorized_twitter_users");

            migrationBuilder.DropColumn(
                name: "InvitesSent",
                table: "authorized_twitter_users");
        }
    }
}
