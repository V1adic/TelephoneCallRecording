using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TelephoneCallRecording.Services.DataBase.Migrations
{
    public partial class InitAuthSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Подключаем расширение citext
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS citext;");

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy",
                            Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),

                    email = table.Column<string>(type: "citext", nullable: false),
                    username = table.Column<string>(type: "citext", maxLength: 15, nullable: false),

                    password_hash = table.Column<string>(maxLength: 44, nullable: false),
                    password_salt = table.Column<string>(maxLength: 24, nullable: false),

                    is_email_confirmed = table.Column<bool>(nullable: false, defaultValue: false),

                    email_confirmation_code_hash = table.Column<string>(maxLength: 44, nullable: true),
                    email_confirmation_expires = table.Column<DateTime>(nullable: true),

                    failed_login_attempts = table.Column<int>(nullable: false, defaultValue: 0),
                    failed_email_confirm_attempts = table.Column<int>(nullable: false, defaultValue: 0),

                    lockout_end = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_username",
                table: "users",
                column: "username",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
