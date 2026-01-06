using Market.Models;
using Microsoft.EntityFrameworkCore;

namespace Market.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<VehicleDetails> VehicleDetails { get; set; }
        public DbSet<PartDetails> PartDetails { get; set; }
        public DbSet<AnnouncementFeature> AnnouncementFeatures { get; set; }
        public DbSet<SystemLog> SystemLogs { get; set; }
        public DbSet<Favorite> Favorites { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Announcement>()
                .HasOne(a => a.User)
                .WithMany(u => u.Announcements)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Announcement>()
                .HasOne(a => a.VehicleDetails)
                .WithOne(v => v.Announcement)
                .HasForeignKey<VehicleDetails>(v => v.AnnouncementId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Announcement>()
                .HasOne(a => a.PartDetails)
                .WithOne(p => p.Announcement)
                .HasForeignKey<PartDetails>(p => p.AnnouncementId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Announcement>()
                .HasMany(a => a.Features)
                .WithOne(f => f.Announcement)
                .HasForeignKey(f => f.AnnouncementId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Announcement>()
                .Property(a => a.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Favorite>()
                .HasKey(f => new { f.UserId, f.AnnouncementId });

            modelBuilder.Entity<Favorite>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict); 
                                                    
            modelBuilder.Entity<Favorite>()
                .HasOne(f => f.Announcement)
                .WithMany()
                .HasForeignKey(f => f.AnnouncementId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}