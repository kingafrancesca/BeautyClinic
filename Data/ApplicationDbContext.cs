using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BeautyClinic.Models;

namespace BeautyClinic.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Service> Services { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Disposition> Dispositions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Service>().Property(s => s.Price).HasPrecision(18, 2);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Service)
                .WithMany()
                .HasForeignKey(b => b.ServiceId);
        }
    }
}