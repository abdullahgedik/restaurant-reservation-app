using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RestaurantReservationSystem.Models;

namespace RestaurantReservationSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Restaurant> Restaurants { get; set; }
        public DbSet<Table> Tables { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Favorite> Favorites { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Restaurant>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Restaurant>()
                .HasMany(r => r.Tables)
                .WithOne(t => t.Restaurant)
                .HasForeignKey(t => t.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Restaurant>()
                .HasMany(r => r.Reservations)
                .WithOne(res => res.Restaurant)
                .HasForeignKey(res => res.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Restaurant>()
                .HasMany(r => r.Reviews)
                .WithOne(rev => rev.Restaurant)
                .HasForeignKey(rev => rev.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Table>()
                .HasMany(t => t.Reservations)
                .WithOne(r => r.Table)
                .HasForeignKey(r => r.TableId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Reservation>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Reservation>()
                .HasOne(r => r.Restaurant)
                .WithMany(r => r.Reservations)
                .HasForeignKey(r => r.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Reservation>()
                .HasOne(r => r.Table)
                .WithMany(t => t.Reservations)
                .HasForeignKey(r => r.TableId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}