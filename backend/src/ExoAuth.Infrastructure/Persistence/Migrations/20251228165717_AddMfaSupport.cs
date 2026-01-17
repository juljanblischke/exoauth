using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExoAuth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMfaSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "mfa_enabled",
                table: "system_users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "mfa_enabled_at",
                table: "system_users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "mfa_secret",
                table: "system_users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "preferred_language",
                table: "system_users",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "en");

            migrationBuilder.CreateTable(
                name: "mfa_backup_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_used = table.Column<bool>(type: "boolean", nullable: false),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_mfa_backup_codes", x => x.id);
                    table.ForeignKey(
                        name: "f_k_mfa_backup_codes_system_users_user_id",
                        column: x => x.user_id,
                        principalTable: "system_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_system_users_mfa_enabled",
                table: "system_users",
                column: "mfa_enabled");

            migrationBuilder.CreateIndex(
                name: "i_x__mfa_backup_codes__user_id__is_used",
                table: "mfa_backup_codes",
                columns: new[] { "user_id", "is_used" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mfa_backup_codes");

            migrationBuilder.DropIndex(
                name: "i_x_system_users_mfa_enabled",
                table: "system_users");

            migrationBuilder.DropColumn(
                name: "mfa_enabled",
                table: "system_users");

            migrationBuilder.DropColumn(
                name: "mfa_enabled_at",
                table: "system_users");

            migrationBuilder.DropColumn(
                name: "mfa_secret",
                table: "system_users");

            migrationBuilder.DropColumn(
                name: "preferred_language",
                table: "system_users");
        }
    }
}
