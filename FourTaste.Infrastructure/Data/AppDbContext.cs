using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Services;
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

        public DbSet<AllergenCatalogItem> AllergenCatalogItems { get; set; } = null!;

        public DbSet<IngredientSubstitute> IngredientSubstitutes { get; set; } = null!;

        public DbSet<UserRecipeSubstitution> UserRecipeSubstitutions { get; set; } = null!;

        public AppDbContext()
        {
        }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
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

                    if (this.Database.IsRelational())
                    {
                        this.Database.ExecuteSqlRaw(@"ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""Avatar"" bytea NULL;");
                        this.Database.ExecuteSqlRaw(@"ALTER TABLE ""Recipes"" ADD COLUMN IF NOT EXISTS ""Photo"" bytea NULL;");
                        this.Database.ExecuteSqlRaw(@"ALTER TABLE ""Recipes"" ADD COLUMN IF NOT EXISTS ""Servings"" INTEGER NOT NULL DEFAULT 1;");

                        // Холодильник
                        this.Database.ExecuteSqlRaw(@"
                            CREATE TABLE IF NOT EXISTS ""UserFridgeItems"" (
                                ""Id"" SERIAL PRIMARY KEY,
                                ""UserId"" INTEGER NOT NULL,
                                ""IngredientName"" TEXT NOT NULL,
                                ""Quantity"" INTEGER NOT NULL DEFAULT 1,
                                ""Unit"" TEXT NOT NULL DEFAULT 'г'
                            );
                        ");
                        this.Database.ExecuteSqlRaw(@"ALTER TABLE ""UserFridgeItems"" ADD COLUMN IF NOT EXISTS ""Quantity"" INTEGER NOT NULL DEFAULT 1;");
                        this.Database.ExecuteSqlRaw(@"ALTER TABLE ""UserFridgeItems"" ADD COLUMN IF NOT EXISTS ""Unit"" TEXT NOT NULL DEFAULT 'г';");

                        // Список покупок
                        this.Database.ExecuteSqlRaw(@"
                            CREATE TABLE IF NOT EXISTS ""UserShoppingItems"" (
                                ""Id"" SERIAL PRIMARY KEY,
                                ""UserId"" INTEGER NOT NULL,
                                ""IngredientName"" TEXT NOT NULL,
                                ""Quantity"" INTEGER NOT NULL DEFAULT 1,
                                ""Unit"" TEXT NOT NULL DEFAULT 'г'
                            );
                        ");

                        // Довідник алергенів
                        this.Database.ExecuteSqlRaw(@"
                            CREATE TABLE IF NOT EXISTS ""AllergenCatalogItems"" (
                                ""Id"" SERIAL PRIMARY KEY,
                                ""Code"" TEXT NOT NULL,
                                ""NameUk"" TEXT NOT NULL,
                                ""Keywords"" TEXT NOT NULL
                            );
                        ");

                        // Довідник замінників
                        this.Database.ExecuteSqlRaw(@"
                            CREATE TABLE IF NOT EXISTS ""IngredientSubstitutes"" (
                                ""Id"" SERIAL PRIMARY KEY,
                                ""OriginalIngredientName"" TEXT NOT NULL,
                                ""SubstituteName"" TEXT NOT NULL,
                                ""Ratio"" DOUBLE PRECISION NOT NULL DEFAULT 1.0,
                                ""Note"" TEXT NULL,
                                ""AllergenCodes"" TEXT NOT NULL DEFAULT '',
                                ""IsVerified"" BOOLEAN NOT NULL DEFAULT TRUE
                            );
                        ");

                        // Персональні підміни користувача
                        this.Database.ExecuteSqlRaw(@"
                            CREATE TABLE IF NOT EXISTS ""UserRecipeSubstitutions"" (
                                ""Id"" SERIAL PRIMARY KEY,
                                ""UserId"" INTEGER NOT NULL,
                                ""RecipeId"" INTEGER NOT NULL,
                                ""OriginalIngredientId"" INTEGER NOT NULL,
                                ""OriginalIngredientName"" TEXT NOT NULL,
                                ""SubstituteIngredientName"" TEXT NOT NULL,
                                ""CalculatedAmount"" TEXT NOT NULL,
                                ""Ratio"" DOUBLE PRECISION NOT NULL DEFAULT 1.0,
                                ""Note"" TEXT NULL
                            );
                        ");
                    }

                    var existingAllergenCodes = this.AllergenCatalogItems.Select(a => a.Code).ToHashSet();
                    var newAllergens = SubstituteSeedData.GetAllergens()
                        .Where(a => !existingAllergenCodes.Contains(a.Code))
                        .ToList();

                    if (newAllergens.Count > 0)
                    {
                        this.AllergenCatalogItems.AddRange(newAllergens);
                        this.SaveChanges();
                    }

                    var existingSubstitutes = this.IngredientSubstitutes
                        .Select(s => s.SubstituteName + "_" + s.OriginalIngredientName)
                        .ToHashSet();

                    var newSubstitutes = SubstituteSeedData.GetSubstitutes()
                        .Where(s => !existingSubstitutes.Contains(s.SubstituteName + "_" + s.OriginalIngredientName))
                        .ToList();

                    if (newSubstitutes.Count > 0)
                    {
                        this.IngredientSubstitutes.AddRange(newSubstitutes);
                        this.SaveChanges();
                    }

                    isDatabaseInitialized = true;
                }
            }
        }
    }
}
