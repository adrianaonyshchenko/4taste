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

        public DbSet<RecipeReview> RecipeReviews { get; set; } = null!;

        public DbSet<ReviewTag> ReviewTags { get; set; } = null!;

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
                        this.Database.ExecuteSqlRaw(@"ALTER TABLE ""Recipes"" ADD COLUMN IF NOT EXISTS ""PhotoThumbnail"" bytea NULL;");

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

                        this.Database.ExecuteSqlRaw(@"
                            CREATE TABLE IF NOT EXISTS ""UserShoppingItems"" (
                                ""Id"" SERIAL PRIMARY KEY,
                                ""UserId"" INTEGER NOT NULL,
                                ""IngredientName"" TEXT NOT NULL,
                                ""Quantity"" INTEGER NOT NULL DEFAULT 1,
                                ""Unit"" TEXT NOT NULL DEFAULT 'г'
                            );
                        ");

                        this.Database.ExecuteSqlRaw(@"
                            CREATE TABLE IF NOT EXISTS ""RecipeReviews"" (
                                ""Id"" SERIAL PRIMARY KEY,
                                ""RecipeId"" INTEGER NOT NULL REFERENCES ""Recipes""(""Id"") ON DELETE CASCADE,
                                ""UserId"" INTEGER NOT NULL REFERENCES ""Users""(""ID"") ON DELETE CASCADE,
                                ""Rating"" INTEGER NOT NULL,
                                ""Text"" TEXT NOT NULL DEFAULT '',
                                ""CreatedAt"" TIMESTAMPTZ NOT NULL DEFAULT NOW()
                            );
                        ");
                        this.Database.ExecuteSqlRaw(@"ALTER TABLE ""RecipeReviews"" ADD COLUMN IF NOT EXISTS ""Photo"" bytea NULL;");
                        this.Database.ExecuteSqlRaw(@"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_RecipeReviews_RecipeId_UserId"" ON ""RecipeReviews"" (""RecipeId"", ""UserId"");");
                        this.Database.ExecuteSqlRaw(@"
                            CREATE TABLE IF NOT EXISTS ""ReviewTags"" (
                                ""Id"" SERIAL PRIMARY KEY,
                                ""ReviewId"" INTEGER NOT NULL REFERENCES ""RecipeReviews""(""Id"") ON DELETE CASCADE,
                                ""Name"" TEXT NOT NULL
                            );
                        ");
                    }

                    isDatabaseInitialized = true;
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<RecipeReview>(entity =>
            {
                entity.HasIndex(r => new { r.RecipeId, r.UserId }).IsUnique();

                entity.HasMany(r => r.Tags)
                    .WithOne()
                    .HasForeignKey(t => t.ReviewId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Recipe>()
                    .WithMany()
                    .HasForeignKey(r => r.RecipeId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
