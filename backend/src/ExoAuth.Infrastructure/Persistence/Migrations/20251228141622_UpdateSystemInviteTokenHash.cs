using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExoAuth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSystemInviteTokenHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "token",
                table: "system_invites",
                newName: "token_hash");

            migrationBuilder.RenameIndex(
                name: "i_x_system_invites_token",
                table: "system_invites",
                newName: "i_x_system_invites_token_hash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "token_hash",
                table: "system_invites",
                newName: "token");

            migrationBuilder.RenameIndex(
                name: "i_x_system_invites_token_hash",
                table: "system_invites",
                newName: "i_x_system_invites_token");
        }
    }
}
