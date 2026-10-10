namespace FourTaste.Infrastructure.Data
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using FourTaste.ApplicationCore.Entities;
    using Microsoft.EntityFrameworkCore;

    internal class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Database=FourTaste;Username=postgres;Password=23072007");
        }
    }
}
