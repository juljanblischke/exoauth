using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExoAuth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInviteRevokedAndResentAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "resent_at",
                table: "system_invites",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "revoked_at",
                table: "system_invites",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "i_x_system_invites_revoked_at",
                table: "system_invites",
                column: "revoked_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "i_x_system_invites_revoked_at",
                table: "system_invites");

            migrationBuilder.DropColumn(
                name: "resent_at",
                table: "system_invites");

            migrationBuilder.DropColumn(
                name: "revoked_at",
                table: "system_invites");
        }
    }
}
