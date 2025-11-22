using Microsoft.EntityFrameworkCore;

namespace SmartTripApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> opt) : base(opt) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RoutePlan> RoutePlans => Set<RoutePlan>();

    protected override void OnModelCreating(ModelBuilder m)
    {
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
        m.Entity<RoutePlan>(e =>
        {
            e.ToTable("routes"); // tablo adı

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Region).HasColumnName("region");
            e.Property(x => x.Days).HasColumnName("days");
            e.Property(x => x.Theme).HasColumnName("theme");
            e.Property(x => x.Budget).HasColumnName("budget");
            e.Property(x => x.Intensity).HasColumnName("intensity");
            e.Property(x => x.Transport).HasColumnName("transport");
            e.Property(x => x.PlanName).HasColumnName("plan_name");
            e.Property(x => x.PlanJson).HasColumnName("plan_json");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });
    }
}
