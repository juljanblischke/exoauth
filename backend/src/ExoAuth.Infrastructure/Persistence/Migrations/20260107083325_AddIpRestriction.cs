using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExoAuth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIpRestriction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ip_restrictions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ip_address = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    source = table.Column<int>(type: "integer", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_ip_restrictions", x => x.id);
                    table.ForeignKey(
                        name: "f_k_ip_restrictions_system_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "system_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_ip_restrictions_created_by_user_id",
                table: "ip_restrictions",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "i_x_ip_restrictions_expires_at",
                table: "ip_restrictions",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "i_x_ip_restrictions_ip_address",
                table: "ip_restrictions",
                column: "ip_address");

            migrationBuilder.CreateIndex(
                name: "i_x_ip_restrictions_source",
                table: "ip_restrictions",
                column: "source");

            migrationBuilder.CreateIndex(
                name: "i_x_ip_restrictions_type",
                table: "ip_restrictions",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "i_x_ip_restrictions_type_expires_at",
                table: "ip_restrictions",
                columns: new[] { "type", "expires_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ip_restrictions");
        }
    }
}
