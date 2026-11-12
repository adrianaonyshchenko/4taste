using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Services;
using FourTaste.Infrastructure.Data;
using FourTaste.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace FourTaste.WPF
{
    public partial class App : Application
    {
        public static IServiceProvider? ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            this.DispatcherUnhandledException += this.App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += this.CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += this.TaskScheduler_UnobservedTaskException;

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            var services = new ServiceCollection();
            ConfigureServices(services);

            ServiceProvider = services.BuildServiceProvider();

            var provider = ServiceProvider;
            _ = Task.Run(async () =>
            {
                try
                {
                    var factory = provider.GetRequiredService<IDbContextFactory<AppDbContext>>();
                    using var ctx = await factory.CreateDbContextAsync();
                    ctx.EnsureDatabaseReady();
                    await BackfillRecipeThumbnailsAsync(provider);

                    var subRepo = provider.GetRequiredService<FourTaste.ApplicationCore.Interfaces.ISubstituteRepository>();
                    await subRepo.GetAllergenCatalogAsync();
                    await subRepo.GetAllSubstitutesAsync();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Не вдалося виконати прогрів бази даних");
                }
            });

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (ServiceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }

            Log.CloseAndFlush();
            base.OnExit(e);
        }

        private static async Task BackfillRecipeThumbnailsAsync(IServiceProvider provider)
        {
            var factory = provider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            var generator = provider.GetRequiredService<IImageThumbnailGenerator>();

            using var listContext = await factory.CreateDbContextAsync();
            var ids = await listContext.Recipes
                .AsNoTracking()
                .Where(r => r.Photo != null && r.PhotoThumbnail == null)
                .Select(r => r.Id)
                .ToListAsync();

            foreach (int id in ids)
            {
                using var context = await factory.CreateDbContextAsync();
                var recipe = await context.Recipes.FirstOrDefaultAsync(r => r.Id == id);
                if (recipe?.Photo is null)
                {
                    continue;
                }

                var thumbnail = generator.CreateThumbnail(recipe.Photo);
                if (thumbnail is null)
                {
                    Log.Warning("Не вдалося створити мініатюру для рецепта {RecipeId}", id);
                    continue;
                }

                recipe.PhotoThumbnail = thumbnail;
                await context.SaveChangesAsync();
            }

            if (ids.Count > 0)
            {
                Log.Information("Створено мініатюри для {Count} рецептів", ids.Count);
            }
        }
        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging();

            services.AddSingleton<ILogger>(_ => Log.Logger);
            string connectionString = "Host=ep-mute-sea-b5ks3hrm-pooler.c-7.us-east-2.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_a8u2btDTpHiX;Ssl Mode=Require;Trust Server Certificate=true";

            services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString), ServiceLifetime.Transient);
            services.AddDbContextFactory<AppDbContext>(options => options.UseNpgsql(connectionString));

            services.AddScoped<IRepository<Allergy>, AllergyRepository>();
            services.AddScoped<IAllergyService, AllergyService>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<UserService>();
            services.AddScoped<AuthService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<MealPlanRepository>();
            services.AddScoped<IMealPlanRepository, MealPlanRepository>();
            services.AddScoped<MealPlanService>();

            services.AddScoped<IRecipeRepository, RecipeRepository>();
            services.AddScoped<IRecipeService, RecipeService>();
            services.AddScoped<ISubstituteRepository, SubstituteRepository>();
            services.AddScoped<ISubstituteService, SubstituteService>();
            services.AddScoped<IRoomMealPlanRepository, RoomMealPlanRepository>();
            services.AddScoped<IRoomMealPlanService, RoomMealPlanService>();

            services.AddTransient<ManageAllergiesWindow>();
            services.AddTransient<RegisterWindow>();
            services.AddTransient<LoginWindow>();
            services.AddTransient<UserProfileWindow>();
            services.AddTransient<EditProfileWindow>();
            services.AddTransient<CookingHistoryWindow>();
            services.AddTransient<CreateRecipeWindow>();
            services.AddTransient<PublicRecipesWindow>();
            services.AddTransient<MyRecipesWindow>();
            services.AddTransient<EditRecipeWindow>();
            services.AddTransient<EditIngredientsWindow>();
            services.AddTransient<IFridgeRepository, FridgeRepository>();
            services.AddTransient<IFridgeService, FridgeService>();
            services.AddTransient<IFavoriteRepository, FavoriteRepository>();
            services.AddScoped<IReviewRepository, ReviewRepository>();
            services.AddScoped<IReviewService, ReviewService>();
            services.AddScoped<IRoomRepository, RoomRepository>();
            services.AddScoped<IRoomService, RoomService>();
            services.AddTransient<CreateRoomWindow>();
            services.AddTransient<RoomDetailsWindow>();
            services.AddSingleton<IImageThumbnailGenerator, WpfImageThumbnailGenerator>();
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error(e.Exception, "Непередбачувана критична помилка в UI потоці (DispatcherUnhandledException)");
            MessageBox.Show(
                $"Виникла критична технічна помилка:\n{e.Exception.Message}\n\nЗверніться до адміністратора або перевірте підключення до бази даних чи мережі.",
                "Критична помилка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                Log.Fatal(ex, "Критична неперехоплена помилка домену застосунку (AppDomain.UnhandledException)");
            }
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            Log.Error(e.Exception, "Неперехоплена помилка фонової задачі (TaskScheduler.UnobservedTaskException)");
            e.SetObserved();
        }
    }
}

