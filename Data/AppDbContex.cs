using Microsoft.EntityFrameworkCore;
using StrideVault.Models;

namespace StrideVault.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Activity> Activities { get; set; }

        public DbSet<ActivityDetails> ActivityDetails { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Activity>()
                .HasOne(a => a.Details)
                .WithOne(d => d.Activity)
                .HasForeignKey<Activity>(a => a.DetailsId)
                .IsRequired(false); // <- allow NULL
        }
    }
}
