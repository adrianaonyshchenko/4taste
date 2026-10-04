using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using FourTaste.ApplicationCore.Entities;


namespace FourTaste.Infrastructure.Data
{
    internal class AppDbContext: DbContext
    {
        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Database=FourTaste;Username=postgres;Password=23072007");
        }
    }
}
