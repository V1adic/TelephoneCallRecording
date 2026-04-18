using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TelephoneCallRecording.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingMvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "subscriber_id",
                table: "users",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "cities",
                columns: table => new
                {
                    city_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    day_tariff = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    night_tariff = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cities", x => x.city_id);
                });

            migrationBuilder.CreateTable(
                name: "city_discounts",
                columns: table => new
                {
                    city_discount_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    city_id = table.Column<int>(type: "integer", nullable: false),
                    min_minutes = table.Column<int>(type: "integer", nullable: false),
                    max_minutes = table.Column<int>(type: "integer", nullable: true),
                    discount_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_city_discounts", x => x.city_discount_id);
                    table.ForeignKey(
                        name: "FK_city_discounts_cities_city_id",
                        column: x => x.city_id,
                        principalTable: "cities",
                        principalColumn: "city_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "subscribers",
                columns: table => new
                {
                    subscriber_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    inn = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    address = table.Column<string>(type: "text", nullable: false),
                    city_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscribers", x => x.subscriber_id);
                    table.ForeignKey(
                        name: "FK_subscribers_cities_city_id",
                        column: x => x.city_id,
                        principalTable: "cities",
                        principalColumn: "city_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "calls",
                columns: table => new
                {
                    call_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    subscriber_id = table.Column<int>(type: "integer", nullable: false),
                    city_id = table.Column<int>(type: "integer", nullable: false),
                    dest_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    start_unix_time = table.Column<long>(type: "bigint", nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    time_of_day = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_calls", x => x.call_id);
                    table.ForeignKey(
                        name: "FK_calls_cities_city_id",
                        column: x => x.city_id,
                        principalTable: "cities",
                        principalColumn: "city_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_calls_subscribers_subscriber_id",
                        column: x => x.subscriber_id,
                        principalTable: "subscribers",
                        principalColumn: "subscriber_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "cities",
                columns: new[] { "city_id", "name", "day_tariff", "night_tariff" },
                values: new object[,]
                {
                    { 1, "Warsaw", 1.20m, 0.70m },
                    { 2, "Krakow", 1.10m, 0.60m },
                    { 3, "Gdansk", 1.30m, 0.80m }
                });

            migrationBuilder.InsertData(
                table: "city_discounts",
                columns: new[] { "city_discount_id", "city_id", "min_minutes", "max_minutes", "discount_percent" },
                values: new object[,]
                {
                    { 1, 1, 0, 10, 0m },
                    { 2, 1, 10, 30, 5m },
                    { 3, 1, 30, 60, 10m },
                    { 4, 1, 60, null, 20m },
                    { 5, 2, 0, 15, 0m },
                    { 6, 2, 15, 40, 7m },
                    { 7, 2, 40, null, 15m },
                    { 8, 3, 0, 20, 0m },
                    { 9, 3, 20, 50, 8m },
                    { 10, 3, 50, null, 18m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_subscriber_id",
                table: "users",
                column: "subscriber_id",
                unique: true,
                filter: "\"subscriber_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_calls_city_id",
                table: "calls",
                column: "city_id");

            migrationBuilder.CreateIndex(
                name: "IX_calls_subscriber_id_dest_phone",
                table: "calls",
                columns: new[] { "subscriber_id", "dest_phone" });

            migrationBuilder.CreateIndex(
                name: "IX_calls_subscriber_id_dest_phone_duration_minutes",
                table: "calls",
                columns: new[] { "subscriber_id", "dest_phone", "duration_minutes" },
                unique: true,
                filter: "\"duration_minutes\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_cities_name",
                table: "cities",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_city_discounts_city_id_min_minutes",
                table: "city_discounts",
                columns: new[] { "city_id", "min_minutes" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscribers_city_id",
                table: "subscribers",
                column: "city_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscribers_phone_number",
                table: "subscribers",
                column: "phone_number",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_users_subscribers_subscriber_id",
                table: "users",
                column: "subscriber_id",
                principalTable: "subscribers",
                principalColumn: "subscriber_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_subscribers_subscriber_id",
                table: "users");

            migrationBuilder.DropTable(
                name: "calls");

            migrationBuilder.DropTable(
                name: "city_discounts");

            migrationBuilder.DropTable(
                name: "subscribers");

            migrationBuilder.DropTable(
                name: "cities");

            migrationBuilder.DropIndex(
                name: "IX_users_subscriber_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "subscriber_id",
                table: "users");
        }
    }
}
