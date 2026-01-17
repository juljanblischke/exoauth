using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExoAuth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "email_announcements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    html_body = table.Column<string>(type: "text", nullable: false),
                    plain_text_body = table.Column<string>(type: "text", nullable: true),
                    target_type = table.Column<int>(type: "integer", nullable: false),
                    target_permission = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    target_user_ids = table.Column<string>(type: "text", nullable: true),
                    total_recipients = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    sent_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    failed_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_email_announcements", x => x.id);
                    table.ForeignKey(
                        name: "f_k_email_announcements_system_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "system_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "email_configuration",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    max_retries_per_provider = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    initial_retry_delay_ms = table.Column<int>(type: "integer", nullable: false, defaultValue: 1000),
                    max_retry_delay_ms = table.Column<int>(type: "integer", nullable: false, defaultValue: 60000),
                    backoff_multiplier = table.Column<double>(type: "double precision", nullable: false, defaultValue: 2.0),
                    circuit_breaker_failure_threshold = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    circuit_breaker_window_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    circuit_breaker_open_duration_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    auto_retry_dlq = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    dlq_retry_interval_hours = table.Column<int>(type: "integer", nullable: false, defaultValue: 6),
                    emails_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    test_mode = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_email_configuration", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "email_providers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    configuration_encrypted = table.Column<string>(type: "text", nullable: false),
                    failure_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_failure_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    circuit_breaker_open_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    total_sent = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_failed = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_success_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_email_providers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "email_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recipient_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    template_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    template_variables = table.Column<string>(type: "text", nullable: true),
                    language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    retry_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    sent_via_provider_id = table.Column<Guid>(type: "uuid", nullable: true),
                    queued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    moved_to_dlq_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    announcement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_email_logs", x => x.id);
                    table.ForeignKey(
                        name: "f_k_email_logs_email_announcements_announcement_id",
                        column: x => x.announcement_id,
                        principalTable: "email_announcements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "f_k_email_logs_email_providers_sent_via_provider_id",
                        column: x => x.sent_via_provider_id,
                        principalTable: "email_providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "f_k_email_logs_system_users_recipient_user_id",
                        column: x => x.recipient_user_id,
                        principalTable: "system_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_email_announcements_created_at",
                table: "email_announcements",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "i_x_email_announcements_created_by_user_id",
                table: "email_announcements",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "i_x_email_announcements_sent_at",
                table: "email_announcements",
                column: "sent_at");

            migrationBuilder.CreateIndex(
                name: "i_x_email_announcements_status",
                table: "email_announcements",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "i_x_email_logs_announcement_id",
                table: "email_logs",
                column: "announcement_id");

            migrationBuilder.CreateIndex(
                name: "i_x_email_logs_queued_at",
                table: "email_logs",
                column: "queued_at");

            migrationBuilder.CreateIndex(
                name: "i_x_email_logs_recipient_email",
                table: "email_logs",
                column: "recipient_email");

            migrationBuilder.CreateIndex(
                name: "i_x_email_logs_recipient_user_id",
                table: "email_logs",
                column: "recipient_user_id");

            migrationBuilder.CreateIndex(
                name: "i_x_email_logs_sent_at",
                table: "email_logs",
                column: "sent_at");

            migrationBuilder.CreateIndex(
                name: "i_x_email_logs_sent_via_provider_id",
                table: "email_logs",
                column: "sent_via_provider_id");

            migrationBuilder.CreateIndex(
                name: "i_x_email_logs_status",
                table: "email_logs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "i_x_email_logs_status_queued_at",
                table: "email_logs",
                columns: new[] { "status", "queued_at" });

            migrationBuilder.CreateIndex(
                name: "i_x_email_logs_template_name",
                table: "email_logs",
                column: "template_name");

            migrationBuilder.CreateIndex(
                name: "i_x_email_providers_is_enabled",
                table: "email_providers",
                column: "is_enabled");

            migrationBuilder.CreateIndex(
                name: "i_x_email_providers_is_enabled_priority",
                table: "email_providers",
                columns: new[] { "is_enabled", "priority" });

            migrationBuilder.CreateIndex(
                name: "i_x_email_providers_priority",
                table: "email_providers",
                column: "priority");

            migrationBuilder.CreateIndex(
                name: "i_x_email_providers_type",
                table: "email_providers",
                column: "type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_configuration");

            migrationBuilder.DropTable(
                name: "email_logs");

            migrationBuilder.DropTable(
                name: "email_announcements");

            migrationBuilder.DropTable(
                name: "email_providers");
        }
    }
}
