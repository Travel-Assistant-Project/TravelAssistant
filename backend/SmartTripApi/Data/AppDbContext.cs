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
    public DbSet<PlacePhoto> PlacePhotos => Set<PlacePhoto>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<GoogleReview> GoogleReviews => Set<GoogleReview>();
    public DbSet<Favorite> Favorites => Set<Favorite>();

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
            e.Property(x => x.Status).HasColumnName("status");
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
            e.Property(x => x.GooglePlaceId).HasColumnName("google_place_id");
            e.Property(x => x.FormattedAddress).HasColumnName("formatted_address");
            e.Property(x => x.UserRatingsTotal).HasColumnName("user_ratings_total");
            e.Property(x => x.PriceLevel).HasColumnName("price_level");
            e.Property(x => x.OpeningHours).HasColumnName("opening_hours").HasColumnType("jsonb");
            e.Property(x => x.PhotoUrls).HasColumnName("photo_urls");
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

        // Place Photos
        m.Entity<PlacePhoto>(e =>
        {
            e.ToTable("place_photos");
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.PlaceId).HasColumnName("place_id");
            e.Property(x => x.ImageUrl).HasColumnName("image_url");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");

            e.HasOne(x => x.Place)
                .WithMany(p => p.PlacePhotos)
                .HasForeignKey(x => x.PlaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Reviews
        m.Entity<Review>(e =>
        {
            e.ToTable("reviews");
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.PlaceId).HasColumnName("place_id");
            e.Property(x => x.Comment).HasColumnName("comment");
            e.Property(x => x.Rating).HasColumnName("rating");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");

            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Place)
                .WithMany()
                .HasForeignKey(x => x.PlaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Google Reviews
        m.Entity<GoogleReview>(e =>
        {
            e.ToTable("google_reviews");
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.PlaceId).HasColumnName("place_id");
            e.Property(x => x.AuthorName).HasColumnName("author_name");
            e.Property(x => x.Comment).HasColumnName("comment");
            e.Property(x => x.Rating).HasColumnName("rating");
            e.Property(x => x.ProfilePhotoUrl).HasColumnName("profile_photo_url");
            e.Property(x => x.ReviewTime).HasColumnName("review_time");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");

            e.HasOne(x => x.Place)
                .WithMany()
                .HasForeignKey(x => x.PlaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Favorites
        m.Entity<Favorite>(e =>
        {
            e.ToTable("favorites");
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.PlaceId).HasColumnName("place_id");
            e.Property(x => x.ItineraryId).HasColumnName("itinerary_id");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");

            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Place)
                .WithMany()
                .HasForeignKey(x => x.PlaceId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Itinerary)
                .WithMany()
                .HasForeignKey(x => x.ItineraryId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ensure only one of PlaceId or ItineraryId is set
            e.HasCheckConstraint("CK_Favorites_OneReference", 
                "(place_id IS NOT NULL AND itinerary_id IS NULL) OR (place_id IS NULL AND itinerary_id IS NOT NULL)");
        });
    }
}
