using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using SmartTripApi.Models;

#nullable disable

namespace SmartTripApi.Migrations
{
    /// <inheritdoc />
    public partial class AddItineraryStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:budget_level", "high,low,medium")
                .Annotation("Npgsql:Enum:intensity_level", "active,relaxed")
                .Annotation("Npgsql:Enum:theme_type", "beach,food,history,nature,photospot,sea")
                .Annotation("Npgsql:Enum:transport_mode", "car,public_transport,walk")
                .Annotation("Npgsql:Enum:user_role", "admin,user");

            migrationBuilder.CreateTable(
                name: "places",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    category = table.Column<ThemeTypeEnum>(type: "theme_type", nullable: true),
                    latitude = table.Column<decimal>(type: "numeric(10,8)", precision: 10, scale: 8, nullable: true),
                    longitude = table.Column<decimal>(type: "numeric(11,8)", precision: 11, scale: 8, nullable: true),
                    city = table.Column<string>(type: "text", nullable: true),
                    country = table.Column<string>(type: "text", nullable: true),
                    google_maps_url = table.Column<string>(type: "text", nullable: true),
                    google_rating = table.Column<decimal>(type: "numeric(2,1)", precision: 2, scale: 1, nullable: true),
                    google_place_id = table.Column<string>(type: "text", nullable: true),
                    formatted_address = table.Column<string>(type: "text", nullable: true),
                    user_ratings_total = table.Column<int>(type: "integer", nullable: true),
                    price_level = table.Column<int>(type: "integer", nullable: true),
                    opening_hours = table.Column<string>(type: "jsonb", nullable: true),
                    photo_urls = table.Column<string[]>(type: "text[]", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_places", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    password = table.Column<string>(type: "text", nullable: false),
                    age = table.Column<int>(type: "integer", nullable: false),
                    country = table.Column<string>(type: "text", nullable: false),
                    city = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "google_reviews",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    place_id = table.Column<int>(type: "integer", nullable: false),
                    author_name = table.Column<string>(type: "text", nullable: false),
                    comment = table.Column<string>(type: "text", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    profile_photo_url = table.Column<string>(type: "text", nullable: true),
                    review_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_google_reviews", x => x.id);
                    table.ForeignKey(
                        name: "FK_google_reviews_places_place_id",
                        column: x => x.place_id,
                        principalTable: "places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "place_photos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    place_id = table.Column<int>(type: "integer", nullable: false),
                    image_url = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_place_photos", x => x.id);
                    table.ForeignKey(
                        name: "FK_place_photos_places_place_id",
                        column: x => x.place_id,
                        principalTable: "places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "itineraries",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    region = table.Column<string>(type: "text", nullable: false),
                    days_count = table.Column<int>(type: "integer", nullable: false),
                    theme = table.Column<ThemeTypeEnum>(type: "theme_type", nullable: true),
                    budget = table.Column<BudgetLevelEnum>(type: "budget_level", nullable: true),
                    intensity = table.Column<IntensityLevelEnum>(type: "intensity_level", nullable: true),
                    transport = table.Column<TransportModeEnum>(type: "transport_mode", nullable: true),
                    is_ai_generated = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_itineraries", x => x.id);
                    table.ForeignKey(
                        name: "FK_itineraries_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reviews",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    place_id = table.Column<int>(type: "integer", nullable: true),
                    comment = table.Column<string>(type: "text", nullable: true),
                    rating = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reviews", x => x.id);
                    table.ForeignKey(
                        name: "FK_reviews_places_place_id",
                        column: x => x.place_id,
                        principalTable: "places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_reviews_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ai_requests",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    itinerary_id = table.Column<int>(type: "integer", nullable: true),
                    request_payload = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    ai_response = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_requests_itineraries_itinerary_id",
                        column: x => x.itinerary_id,
                        principalTable: "itineraries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ai_requests_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "itinerary_days",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    itinerary_id = table.Column<int>(type: "integer", nullable: false),
                    day_number = table.Column<int>(type: "integer", nullable: false),
                    weather_info = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_itinerary_days", x => x.id);
                    table.ForeignKey(
                        name: "FK_itinerary_days_itineraries_itinerary_id",
                        column: x => x.itinerary_id,
                        principalTable: "itineraries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "activities",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    itinerary_day_id = table.Column<int>(type: "integer", nullable: true),
                    place_id = table.Column<int>(type: "integer", nullable: true),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    start_time = table.Column<TimeSpan>(type: "interval", nullable: true),
                    end_time = table.Column<TimeSpan>(type: "interval", nullable: true),
                    image_urls = table.Column<string[]>(type: "text[]", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activities", x => x.id);
                    table.ForeignKey(
                        name: "FK_activities_itinerary_days_itinerary_day_id",
                        column: x => x.itinerary_day_id,
                        principalTable: "itinerary_days",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_activities_places_place_id",
                        column: x => x.place_id,
                        principalTable: "places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_activities_itinerary_day_id",
                table: "activities",
                column: "itinerary_day_id");

            migrationBuilder.CreateIndex(
                name: "IX_activities_place_id",
                table: "activities",
                column: "place_id");

            migrationBuilder.CreateIndex(
                name: "IX_ai_requests_itinerary_id",
                table: "ai_requests",
                column: "itinerary_id");

            migrationBuilder.CreateIndex(
                name: "IX_ai_requests_user_id",
                table: "ai_requests",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_google_reviews_place_id",
                table: "google_reviews",
                column: "place_id");

            migrationBuilder.CreateIndex(
                name: "IX_itineraries_user_id",
                table: "itineraries",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_itinerary_days_itinerary_id_day_number",
                table: "itinerary_days",
                columns: new[] { "itinerary_id", "day_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_place_photos_place_id",
                table: "place_photos",
                column: "place_id");

            migrationBuilder.CreateIndex(
                name: "IX_reviews_place_id",
                table: "reviews",
                column: "place_id");

            migrationBuilder.CreateIndex(
                name: "IX_reviews_user_id",
                table: "reviews",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "activities");

            migrationBuilder.DropTable(
                name: "ai_requests");

            migrationBuilder.DropTable(
                name: "google_reviews");

            migrationBuilder.DropTable(
                name: "place_photos");

            migrationBuilder.DropTable(
                name: "reviews");

            migrationBuilder.DropTable(
                name: "itinerary_days");

            migrationBuilder.DropTable(
                name: "places");

            migrationBuilder.DropTable(
                name: "itineraries");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
