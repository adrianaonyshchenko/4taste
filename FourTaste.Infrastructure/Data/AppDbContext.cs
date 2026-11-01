using FourTaste.ApplicationCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace FourTaste.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        private static readonly object InitLock = new ();
        private static bool isDatabaseInitialized;

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Allergy> Allergies { get; set; } = null!;
        public DbSet<MealPlan> MealPlans { get; set; } = null!;
        public DbSet<Recipe> Recipes { get; set; } = null!;
        public DbSet<UserFridgeItem> UserFridgeItems { get; set; } = null!;
        public DbSet<UserShoppingItem> UserShoppingItems { get; set; } = null!;

        public AppDbContext()
        {
        }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public void EnsureDatabaseReady()
        {
            if (isDatabaseInitialized)
            {
                return;
            }

            lock (InitLock)
            {
                if (!isDatabaseInitialized)
                {
                    this.Database.EnsureCreated();

                    this.Database.ExecuteSqlRaw(@"ALTER TABLE ""Recipes"" ADD COLUMN IF NOT EXISTS ""Photo"" bytea NULL;");

                    // Таблиця для холодильника
                    this.Database.ExecuteSqlRaw(@"
                        CREATE TABLE IF NOT EXISTS ""UserFridgeItems"" (
                            ""Id"" SERIAL PRIMARY KEY,
                            ""UserId"" INTEGER NOT NULL,
                            ""IngredientName"" TEXT NOT NULL,
                            ""Quantity"" INTEGER NOT NULL DEFAULT 1,
                            ""Unit"" TEXT NOT NULL DEFAULT 'шт'
                        );
                    ");
                    this.Database.ExecuteSqlRaw(@"ALTER TABLE ""UserFridgeItems"" ADD COLUMN IF NOT EXISTS ""Quantity"" INTEGER NOT NULL DEFAULT 1;");
                    this.Database.ExecuteSqlRaw(@"ALTER TABLE ""UserFridgeItems"" ADD COLUMN IF NOT EXISTS ""Unit"" TEXT NOT NULL DEFAULT 'шт';");

                    // Таблиця для списку покупок
                    this.Database.ExecuteSqlRaw(@"
                        CREATE TABLE IF NOT EXISTS ""UserShoppingItems"" (
                            ""Id"" SERIAL PRIMARY KEY,
                            ""UserId"" INTEGER NOT NULL,
                            ""IngredientName"" TEXT NOT NULL,
                            ""Quantity"" INTEGER NOT NULL DEFAULT 1,
                            ""Unit"" TEXT NOT NULL DEFAULT 'шт'
                        );
                    ");

                    isDatabaseInitialized = true;
                }
            }
        }
    }
}