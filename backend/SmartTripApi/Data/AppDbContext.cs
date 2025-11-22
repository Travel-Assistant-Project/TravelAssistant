using Microsoft.EntityFrameworkCore;
using SmartTripApi.Models;

namespace SmartTripApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> opt) : base(opt) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Itinerary> Itineraries => Set<Itinerary>();
    public DbSet<ItineraryDay> ItineraryDays => Set<ItineraryDay>();
    public DbSet<Place> Places => Set<Place>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<AIRequest> AIRequests => Set<AIRequest>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        // Users
        m.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasIndex(x => x.Email).IsUnique();

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Name).HasColumnName("name");
            e.Property(x => x.Email).HasColumnName("email");
            e.Property(x => x.PasswordHash).HasColumnName("password");
            e.Property(x => x.Age).HasColumnName("age");
            e.Property(x => x.Country).HasColumnName("country");
            e.Property(x => x.City).HasColumnName("city");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        // Itineraries
        m.Entity<Itinerary>(e =>
        {
            e.ToTable("itineraries");
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.Name).HasColumnName("name");
            e.Property(x => x.Region).HasColumnName("region");
            e.Property(x => x.DaysCount).HasColumnName("days_count");
            e.Property(x => x.Theme).HasColumnName("theme").HasColumnType("theme_type");
            e.Property(x => x.Budget).HasColumnName("budget").HasColumnType("budget_level");
            e.Property(x => x.Intensity).HasColumnName("intensity").HasColumnType("intensity_level");
            e.Property(x => x.Transport).HasColumnName("transport").HasColumnType("transport_mode");
            e.Property(x => x.IsAiGenerated).HasColumnName("is_ai_generated");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");

            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Itinerary Days
        m.Entity<ItineraryDay>(e =>
        {
            e.ToTable("itinerary_days");
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ItineraryId).HasColumnName("itinerary_id");
            e.Property(x => x.DayNumber).HasColumnName("day_number");
            e.Property(x => x.WeatherInfo).HasColumnName("weather_info").HasColumnType("jsonb");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");

            e.HasOne(x => x.Itinerary)
                .WithMany(i => i.ItineraryDays)
                .HasForeignKey(x => x.ItineraryId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.ItineraryId, x.DayNumber }).IsUnique();
        });

        // Places
        m.Entity<Place>(e =>
        {
            e.ToTable("places");
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Name).HasColumnName("name");
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.Category).HasColumnName("category").HasColumnType("theme_type");
            e.Property(x => x.Latitude).HasColumnName("latitude").HasPrecision(10, 8);
            e.Property(x => x.Longitude).HasColumnName("longitude").HasPrecision(11, 8);
            e.Property(x => x.City).HasColumnName("city");
            e.Property(x => x.Country).HasColumnName("country");
            e.Property(x => x.GoogleMapsUrl).HasColumnName("google_maps_url");
            e.Property(x => x.GoogleRating).HasColumnName("google_rating").HasPrecision(2, 1);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        // Activities
        m.Entity<Activity>(e =>
        {
            e.ToTable("activities");
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ItineraryDayId).HasColumnName("itinerary_day_id");
            e.Property(x => x.PlaceId).HasColumnName("place_id");
            e.Property(x => x.Title).HasColumnName("title");
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.Reason).HasColumnName("reason");
            e.Property(x => x.StartTime).HasColumnName("start_time");
            e.Property(x => x.EndTime).HasColumnName("end_time");
            e.Property(x => x.ImageUrls).HasColumnName("image_urls");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");

            e.HasOne(x => x.ItineraryDay)
                .WithMany(d => d.Activities)
                .HasForeignKey(x => x.ItineraryDayId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Place)
                .WithMany(p => p.Activities)
                .HasForeignKey(x => x.PlaceId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // AI Requests
        m.Entity<AIRequest>(e =>
        {
            e.ToTable("ai_requests");
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.ItineraryId).HasColumnName("itinerary_id");
            e.Property(x => x.RequestPayload).HasColumnName("request_payload").HasColumnType("jsonb");
            e.Property(x => x.AiResponse).HasColumnName("ai_response").HasColumnType("jsonb");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");

            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Itinerary)
                .WithMany()
                .HasForeignKey(x => x.ItineraryId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
