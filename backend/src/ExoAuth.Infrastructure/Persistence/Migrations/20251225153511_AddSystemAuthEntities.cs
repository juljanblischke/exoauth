using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExoAuth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemAuthEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "system_permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_system_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "system_users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    email_verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_system_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_revoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    device_info = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    system_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "f_k_refresh_tokens_system_users_system_user_id",
                        column: x => x.system_user_id,
                        principalTable: "system_users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "system_audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    details = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_system_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "f_k_system_audit_logs_system_users_user_id",
                        column: x => x.user_id,
                        principalTable: "system_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "system_invites",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    permission_ids = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    accepted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    invited_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_system_invites", x => x.id);
                    table.ForeignKey(
                        name: "f_k_system_invites_system_users_invited_by",
                        column: x => x.invited_by,
                        principalTable: "system_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "system_user_permissions",
                columns: table => new
                {
                    system_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    system_permission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    granted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    granted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_system_user_permissions", x => new { x.system_user_id, x.system_permission_id });
                    table.ForeignKey(
                        name: "f_k_system_user_permissions_system_permissions_system_permissio~",
                        column: x => x.system_permission_id,
                        principalTable: "system_permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_system_user_permissions_system_users_system_user_id",
                        column: x => x.system_user_id,
                        principalTable: "system_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_refresh_tokens_expires_at",
                table: "refresh_tokens",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "i_x_refresh_tokens_is_revoked",
                table: "refresh_tokens",
                column: "is_revoked");

            migrationBuilder.CreateIndex(
                name: "i_x_refresh_tokens_system_user_id",
                table: "refresh_tokens",
                column: "system_user_id");

            migrationBuilder.CreateIndex(
                name: "i_x_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash");

            migrationBuilder.CreateIndex(
                name: "i_x_refresh_tokens_user_id",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "i_x_refresh_tokens_user_id_user_type_is_revoked",
                table: "refresh_tokens",
                columns: new[] { "user_id", "user_type", "is_revoked" });

            migrationBuilder.CreateIndex(
                name: "i_x_refresh_tokens_user_type",
                table: "refresh_tokens",
                column: "user_type");

            migrationBuilder.CreateIndex(
                name: "i_x_system_audit_logs_action",
                table: "system_audit_logs",
                column: "action");

            migrationBuilder.CreateIndex(
                name: "i_x_system_audit_logs_action_created_at",
                table: "system_audit_logs",
                columns: new[] { "action", "created_at" });

            migrationBuilder.CreateIndex(
                name: "i_x_system_audit_logs_created_at",
                table: "system_audit_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "i_x_system_audit_logs_entity_type",
                table: "system_audit_logs",
                column: "entity_type");

            migrationBuilder.CreateIndex(
                name: "i_x_system_audit_logs_user_id",
                table: "system_audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "i_x_system_invites_accepted_at",
                table: "system_invites",
                column: "accepted_at");

            migrationBuilder.CreateIndex(
                name: "i_x_system_invites_email",
                table: "system_invites",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "i_x_system_invites_expires_at",
                table: "system_invites",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "i_x_system_invites_invited_by",
                table: "system_invites",
                column: "invited_by");

            migrationBuilder.CreateIndex(
                name: "i_x_system_invites_token",
                table: "system_invites",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_system_permissions_category",
                table: "system_permissions",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "i_x_system_permissions_name",
                table: "system_permissions",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_system_user_permissions_system_permission_id",
                table: "system_user_permissions",
                column: "system_permission_id");

            migrationBuilder.CreateIndex(
                name: "i_x_system_user_permissions_system_user_id",
                table: "system_user_permissions",
                column: "system_user_id");

            migrationBuilder.CreateIndex(
                name: "i_x_system_users_email",
                table: "system_users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_system_users_is_active",
                table: "system_users",
                column: "is_active");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "system_audit_logs");

            migrationBuilder.DropTable(
                name: "system_invites");

            migrationBuilder.DropTable(
                name: "system_user_permissions");

            migrationBuilder.DropTable(
                name: "system_permissions");

            migrationBuilder.DropTable(
                name: "system_users");
        }
    }
}
