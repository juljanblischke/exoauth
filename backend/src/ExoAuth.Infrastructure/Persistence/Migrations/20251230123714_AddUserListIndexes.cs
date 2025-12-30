using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExoAuth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserListIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "i_x_system_users_is_anonymized",
                table: "system_users",
                column: "is_anonymized");

            migrationBuilder.CreateIndex(
                name: "i_x_system_users_locked_until",
                table: "system_users",
                column: "locked_until");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "i_x_system_users_is_anonymized",
                table: "system_users");

            migrationBuilder.DropIndex(
                name: "i_x_system_users_locked_until",
                table: "system_users");
        }
    }
}
