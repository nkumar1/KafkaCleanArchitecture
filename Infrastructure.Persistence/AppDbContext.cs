using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        public DbSet<VehicleLocation> VehicleLocations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<VehicleLocation>()
        .HasKey(v => new { v.VehicleId, v.Timestamp });  // Primary key

            // create a composite index in AppDbContext
            // to ensure that the combination of VehicleId and Timestamp is unique
            modelBuilder.Entity<VehicleLocation>()
            .HasIndex(v => new { v.VehicleId, v.Timestamp })
            .IsUnique();
        }
    }
}
