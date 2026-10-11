using FourTaste.ApplicationCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace FourTaste.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }

        public DbSet<Allergy> Allergies { get; set; }

        public AppDbContext()
        {
        }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql("Host=ep-mute-sea-b5ks3hrm-pooler.c-7.us-east-2.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_a8u2btDTpHiX;Ssl Mode=Require;Trust Server Certificate=true");
            }
        }
    }
}