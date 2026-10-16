using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Models;

namespace RaceDayAPI.Data;

public class RaceDayContext : DbContext
{
    public RaceDayContext(DbContextOptions<RaceDayContext> options) : base(options) { }

    public DbSet<UserProfile> Users => Set<UserProfile>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<Result> Results => Set<Result>();
    public DbSet<Models.Route> Routes => Set<Models.Route>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<UserProfile>().HasIndex(u => u.Email).IsUnique();

        b.Entity<Registration>().HasOne<UserProfile>().WithMany()
          .HasForeignKey(r => r.ParticipantId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Registration>().HasOne<Category>().WithMany()
          .HasForeignKey(r => r.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Registration>().HasOne<Event>().WithMany()
          .HasForeignKey(r => r.EventId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<Result>().HasOne<Registration>().WithMany()
          .HasForeignKey(r => r.RegistrationId);
    }
}