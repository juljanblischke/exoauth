using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExoAuth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "device_session_id",
                table: "refresh_tokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "remember_me",
                table: "refresh_tokens",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "device_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    device_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    device_fingerprint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    browser = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    browser_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    operating_system = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    os_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    device_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    latitude = table.Column<double>(type: "double precision", nullable: true),
                    longitude = table.Column<double>(type: "double precision", nullable: true),
                    is_trusted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    last_activity_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_revoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_device_sessions", x => x.id);
                    table.ForeignKey(
                        name: "f_k_device_sessions_system_users_user_id",
                        column: x => x.user_id,
                        principalTable: "system_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_refresh_tokens_device_session_id",
                table: "refresh_tokens",
                column: "device_session_id");

            migrationBuilder.CreateIndex(
                name: "i_x_device_sessions_device_id",
                table: "device_sessions",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "i_x_device_sessions_is_revoked",
                table: "device_sessions",
                column: "is_revoked");

            migrationBuilder.CreateIndex(
                name: "i_x_device_sessions_last_activity_at",
                table: "device_sessions",
                column: "last_activity_at");

            migrationBuilder.CreateIndex(
                name: "i_x_device_sessions_user_id",
                table: "device_sessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "i_x_device_sessions_user_id_device_id",
                table: "device_sessions",
                columns: new[] { "user_id", "device_id" });

            migrationBuilder.CreateIndex(
                name: "i_x_device_sessions_user_id_is_revoked",
                table: "device_sessions",
                columns: new[] { "user_id", "is_revoked" });

            migrationBuilder.AddForeignKey(
                name: "f_k_refresh_tokens_device_sessions_device_session_id",
                table: "refresh_tokens",
                column: "device_session_id",
                principalTable: "device_sessions",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_refresh_tokens_device_sessions_device_session_id",
                table: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "device_sessions");

            migrationBuilder.DropIndex(
                name: "i_x_refresh_tokens_device_session_id",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "device_session_id",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "remember_me",
                table: "refresh_tokens");
        }
    }
}
