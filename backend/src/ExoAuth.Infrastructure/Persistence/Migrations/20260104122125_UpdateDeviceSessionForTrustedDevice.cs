using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExoAuth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDeviceSessionForTrustedDevice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_trusted",
                table: "device_sessions");

            migrationBuilder.AddColumn<Guid>(
                name: "trusted_device_id",
                table: "device_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "i_x_device_sessions_trusted_device_id",
                table: "device_sessions",
                column: "trusted_device_id");

            migrationBuilder.AddForeignKey(
                name: "f_k_device_sessions_trusted_devices_trusted_device_id",
                table: "device_sessions",
                column: "trusted_device_id",
                principalTable: "trusted_devices",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_device_sessions_trusted_devices_trusted_device_id",
                table: "device_sessions");

            migrationBuilder.DropIndex(
                name: "i_x_device_sessions_trusted_device_id",
                table: "device_sessions");

            migrationBuilder.DropColumn(
                name: "trusted_device_id",
                table: "device_sessions");

            migrationBuilder.AddColumn<bool>(
                name: "is_trusted",
                table: "device_sessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
