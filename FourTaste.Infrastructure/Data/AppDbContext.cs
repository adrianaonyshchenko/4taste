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
        public DbSet<UserFavoriteRecipe> UserFavoriteRecipes { get; set; } = null!;
        public DbSet<RecipeReview> RecipeReviews { get; set; } = null!;
        public DbSet<ReviewTag> ReviewTags { get; set; } = null!;
        public DbSet<AllergenCatalogItem> AllergenCatalogItems { get; set; } = null!;
        public DbSet<IngredientSubstitute> IngredientSubstitutes { get; set; } = null!;
        public DbSet<UserRecipeSubstitution> UserRecipeSubstitutions { get; set; } = null!;
        public DbSet<Room> Rooms { get; set; } = null!;
        public DbSet<RoomMember> RoomMembers { get; set; } = null!;

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
                if (isDatabaseInitialized)
                {
                    return;
                }

                try
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
    CREATE TABLE IF NOT EXISTS ""UserFavoriteRecipes"" (
        ""UserId"" INTEGER NOT NULL,
        ""RecipeId"" INTEGER NOT NULL,
        CONSTRAINT ""PK_UserFavoriteRecipes"" PRIMARY KEY (""UserId"", ""RecipeId"")
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
                        this.Database.ExecuteSqlRaw(@"
    CREATE TABLE IF NOT EXISTS ""AllergenCatalogItems"" (
        ""Id"" SERIAL PRIMARY KEY,
        ""Code"" TEXT NOT NULL,
        ""NameUk"" TEXT NOT NULL,
        ""Keywords"" TEXT NOT NULL
    );
");

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

                        this.Database.ExecuteSqlRaw(@"
                            CREATE TABLE IF NOT EXISTS ""UserFavoriteRecipes"" (
                                ""Id"" SERIAL PRIMARY KEY,
                                ""UserId"" INTEGER NOT NULL,
                                ""RecipeId"" INTEGER NOT NULL
                            );
                        ");

                        this.Database.ExecuteSqlRaw(@"
                            CREATE TABLE IF NOT EXISTS ""Rooms"" (
                                ""Id"" SERIAL PRIMARY KEY,
                                ""Name"" TEXT NOT NULL,
                                ""Description"" TEXT NULL,
                                ""OwnerId"" INTEGER NOT NULL REFERENCES ""Users""(""ID"") ON DELETE CASCADE,
                                ""InviteCode"" TEXT NOT NULL,
                                ""CreatedAt"" TIMESTAMPTZ NOT NULL DEFAULT NOW()
                            );
                        ");
                        this.Database.ExecuteSqlRaw(@"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Rooms_InviteCode"" ON ""Rooms"" (""InviteCode"");");

                        this.Database.ExecuteSqlRaw(@"
                            CREATE TABLE IF NOT EXISTS ""RoomMembers"" (
                                ""Id"" SERIAL PRIMARY KEY,
                                ""RoomId"" INTEGER NOT NULL REFERENCES ""Rooms""(""Id"") ON DELETE CASCADE,
                                ""UserId"" INTEGER NOT NULL REFERENCES ""Users""(""ID"") ON DELETE CASCADE,
                                ""Role"" INTEGER NOT NULL,
                                ""JoinedAt"" TIMESTAMPTZ NOT NULL DEFAULT NOW()
                            );
                        ");
                        this.Database.ExecuteSqlRaw(@"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_RoomMembers_RoomId_UserId"" ON ""RoomMembers"" (""RoomId"", ""UserId"");");
                    }
                    isDatabaseInitialized = true;
                }
                catch (System.Exception ex)
                {
                    Serilog.Log.Error(ex, "Помилка при ініціалізації структури бази даних");
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

            modelBuilder.Entity<Room>(entity =>
            {
                entity.HasIndex(r => r.InviteCode).IsUnique();
                entity.HasMany(r => r.Members)
                    .WithOne()
                    .HasForeignKey(m => m.RoomId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RoomMember>(entity =>
            {
                entity.HasIndex(m => new { m.RoomId, m.UserId }).IsUnique();
            });
        }
    }
}