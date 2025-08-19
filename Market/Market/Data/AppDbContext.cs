using Market.Models;
using Microsoft.EntityFrameworkCore;

namespace Market.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Announcement> Announcements { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Announcement>()
               .HasOne(a => a.User)
               .WithMany(u => u.Announcements)
               .HasForeignKey(a => a.UserId)
               .OnDelete(DeleteBehavior.Cascade); 

            modelBuilder.Entity<Announcement>()
                .Property(a => a.TypeSpecificData)
                .HasColumnType("nvarchar(max)");
        }
    }
}