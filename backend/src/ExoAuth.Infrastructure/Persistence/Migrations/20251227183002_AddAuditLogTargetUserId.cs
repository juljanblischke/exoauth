using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExoAuth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogTargetUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "target_user_id",
                table: "system_audit_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "i_x_system_audit_logs_target_user_id",
                table: "system_audit_logs",
                column: "target_user_id");

            migrationBuilder.AddForeignKey(
                name: "f_k_system_audit_logs_system_users_target_user_id",
                table: "system_audit_logs",
                column: "target_user_id",
                principalTable: "system_users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_system_audit_logs_system_users_target_user_id",
                table: "system_audit_logs");

            migrationBuilder.DropIndex(
                name: "i_x_system_audit_logs_target_user_id",
                table: "system_audit_logs");

            migrationBuilder.DropColumn(
                name: "target_user_id",
                table: "system_audit_logs");
        }
    }
}
