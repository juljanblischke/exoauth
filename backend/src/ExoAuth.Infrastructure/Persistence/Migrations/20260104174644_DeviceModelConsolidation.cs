using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExoAuth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DeviceModelConsolidation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the old FK to device_sessions
            migrationBuilder.DropForeignKey(
                name: "f_k_refresh_tokens_device_sessions_device_session_id",
                table: "refresh_tokens");

            // Drop old index
            migrationBuilder.DropIndex(
                name: "i_x_refresh_tokens_device_session_id",
                table: "refresh_tokens");

            // Rename column from device_session_id to device_id
            migrationBuilder.RenameColumn(
                name: "device_session_id",
                table: "refresh_tokens",
                newName: "device_id");

            // Create the new devices table
            migrationBuilder.CreateTable(
                name: "devices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    fingerprint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
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
                    status = table.Column<int>(type: "integer", nullable: false),
                    trusted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    approval_token_hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    approval_code_hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    approval_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approval_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    risk_score = table.Column<int>(type: "integer", nullable: true),
                    risk_factors = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_devices", x => x.id);
                    table.ForeignKey(
                        name: "f_k_devices_system_users_user_id",
                        column: x => x.user_id,
                        principalTable: "system_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes on devices table
            migrationBuilder.CreateIndex(
                name: "i_x_devices_approval_token_hash",
                table: "devices",
                column: "approval_token_hash");

            migrationBuilder.CreateIndex(
                name: "i_x_devices_device_id",
                table: "devices",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "i_x_devices_last_used_at",
                table: "devices",
                column: "last_used_at");

            migrationBuilder.CreateIndex(
                name: "i_x_devices_status",
                table: "devices",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "i_x_devices_user_id",
                table: "devices",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "i_x_devices_user_id_device_id",
                table: "devices",
                columns: new[] { "user_id", "device_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_devices_user_id_status",
                table: "devices",
                columns: new[] { "user_id", "status" });

            // Create index on refresh_tokens.device_id
            migrationBuilder.CreateIndex(
                name: "i_x_refresh_tokens_device_id",
                table: "refresh_tokens",
                column: "device_id");

            // Note: FK from refresh_tokens.device_id to devices.id is NOT added here
            // because the old device_session_id values won't match new device ids.
            // The FK will need to be added after data migration.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop index on refresh_tokens
            migrationBuilder.DropIndex(
                name: "i_x_refresh_tokens_device_id",
                table: "refresh_tokens");

            // Drop devices table
            migrationBuilder.DropTable(
                name: "devices");

            // Rename column back
            migrationBuilder.RenameColumn(
                name: "device_id",
                table: "refresh_tokens",
                newName: "device_session_id");

            // Recreate old index
            migrationBuilder.CreateIndex(
                name: "i_x_refresh_tokens_device_session_id",
                table: "refresh_tokens",
                column: "device_session_id");

            // Recreate old FK
            migrationBuilder.AddForeignKey(
                name: "f_k_refresh_tokens_device_sessions_device_session_id",
                table: "refresh_tokens",
                column: "device_session_id",
                principalTable: "device_sessions",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
