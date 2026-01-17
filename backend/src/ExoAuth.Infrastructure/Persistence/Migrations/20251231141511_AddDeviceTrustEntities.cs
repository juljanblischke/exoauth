using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExoAuth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceTrustEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "device_approval_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    code_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    risk_score = table.Column<int>(type: "integer", nullable: false),
                    risk_factors = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolved_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "login_patterns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    typical_countries = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, defaultValue: "[]"),
                    typical_cities = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, defaultValue: "[]"),
                    typical_hours = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: "[]"),
                    typical_device_types = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: "[]"),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    last_country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_latitude = table.Column<double>(type: "double precision", nullable: true),
                    last_longitude = table.Column<double>(type: "double precision", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_login_patterns", x => x.id);
                    table.ForeignKey(
                        name: "f_k_login_patterns_system_users_user_id",
                        column: x => x.user_id,
                        principalTable: "system_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "i_x_login_patterns_user_id",
                table: "login_patterns",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "device_approval_requests");

            migrationBuilder.DropTable(
                name: "login_patterns");
        }
    }
}
