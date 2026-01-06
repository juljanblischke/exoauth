using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExoAuth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPasskeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_refresh_tokens_devices_device_id1",
                table: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "device_approval_requests");

            migrationBuilder.DropTable(
                name: "device_sessions");

            migrationBuilder.DropTable(
                name: "trusted_devices");

            migrationBuilder.DropIndex(
                name: "i_x_refresh_tokens_device_id1",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "device_id1",
                table: "refresh_tokens");

            migrationBuilder.CreateTable(
                name: "passkeys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credential_id = table.Column<byte[]>(type: "bytea", maxLength: 1024, nullable: false),
                    public_key = table.Column<byte[]>(type: "bytea", maxLength: 2048, nullable: false),
                    counter = table.Column<long>(type: "bigint", nullable: false),
                    cred_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    aa_guid = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_passkeys", x => x.id);
                    table.ForeignKey(
                        name: "f_k_passkeys_system_users_user_id",
                        column: x => x.user_id,
                        principalTable: "system_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "i_x__passkeys__credential_id",
                table: "passkeys",
                column: "credential_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x__passkeys__user_id",
                table: "passkeys",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "passkeys");

            migrationBuilder.AddColumn<Guid>(
                name: "device_id1",
                table: "refresh_tokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "trusted_devices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    browser = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    browser_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    device_fingerprint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    device_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    device_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    last_city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    operating_system = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    os_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    trusted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_trusted_devices", x => x.id);
                    table.ForeignKey(
                        name: "f_k_trusted_devices_system_users_user_id",
                        column: x => x.user_id,
                        principalTable: "system_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "device_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trusted_device_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    browser = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    browser_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    device_fingerprint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    device_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    device_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    device_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    is_revoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    last_activity_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: true),
                    longitude = table.Column<double>(type: "double precision", nullable: true),
                    operating_system = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    os_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    user_agent = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
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
                    table.ForeignKey(
                        name: "f_k_device_sessions_trusted_devices_trusted_device_id",
                        column: x => x.trusted_device_id,
                        principalTable: "trusted_devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "device_approval_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    code_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolved_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    risk_factors = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    risk_score = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_device_approval_requests", x => x.id);
                    table.ForeignKey(
                        name: "f_k_device_approval_requests_device_sessions_device_session_id",
                        column: x => x.device_session_id,
                        principalTable: "device_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_device_approval_requests_system_users_user_id",
                        column: x => x.user_id,
                        principalTable: "system_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_refresh_tokens_device_id1",
                table: "refresh_tokens",
                column: "device_id1");

            migrationBuilder.CreateIndex(
                name: "i_x_device_approval_requests_code_hash",
                table: "device_approval_requests",
                column: "code_hash");

            migrationBuilder.CreateIndex(
                name: "i_x_device_approval_requests_device_session_id",
                table: "device_approval_requests",
                column: "device_session_id");

            migrationBuilder.CreateIndex(
                name: "i_x_device_approval_requests_device_session_id_status",
                table: "device_approval_requests",
                columns: new[] { "device_session_id", "status" });

            migrationBuilder.CreateIndex(
                name: "i_x_device_approval_requests_expires_at",
                table: "device_approval_requests",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "i_x_device_approval_requests_status",
                table: "device_approval_requests",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "i_x_device_approval_requests_token_hash",
                table: "device_approval_requests",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_device_approval_requests_user_id",
                table: "device_approval_requests",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "i_x_device_approval_requests_user_id_status_expires_at",
                table: "device_approval_requests",
                columns: new[] { "user_id", "status", "expires_at" });

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
                name: "i_x_device_sessions_trusted_device_id",
                table: "device_sessions",
                column: "trusted_device_id");

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

            migrationBuilder.CreateIndex(
                name: "i_x_trusted_devices_device_id",
                table: "trusted_devices",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "i_x_trusted_devices_last_used_at",
                table: "trusted_devices",
                column: "last_used_at");

            migrationBuilder.CreateIndex(
                name: "i_x_trusted_devices_user_id",
                table: "trusted_devices",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "i_x_trusted_devices_user_id_device_fingerprint",
                table: "trusted_devices",
                columns: new[] { "user_id", "device_fingerprint" });

            migrationBuilder.CreateIndex(
                name: "i_x_trusted_devices_user_id_device_id",
                table: "trusted_devices",
                columns: new[] { "user_id", "device_id" });

            migrationBuilder.AddForeignKey(
                name: "f_k_refresh_tokens_devices_device_id1",
                table: "refresh_tokens",
                column: "device_id1",
                principalTable: "devices",
                principalColumn: "id");
        }
    }
}
