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
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "РќРµ РІРґР°Р»РѕСЃСЏ РІРёРєРѕРЅР°С‚Рё РїСЂРѕРіСЂС–РІ Р±Р°Р·Рё РґР°РЅРёС…");
                }
            });

            base.OnStartup(e);
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
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error(e.Exception, "РќРµРїРµСЂРµРґР±Р°С‡СѓРІР°РЅР° РєСЂРёС‚РёС‡РЅР° РїРѕРјРёР»РєР° РІ UI РїРѕС‚РѕС†С– (DispatcherUnhandledException)");
            MessageBox.Show(
                $"Р’РёРЅРёРєР»Р° РєСЂРёС‚РёС‡РЅР° С‚РµС…РЅС–С‡РЅР° РїРѕРјРёР»РєР°:\n{e.Exception.Message}\n\nР—РІРµСЂРЅС–С‚СЊСЃСЏ РґРѕ Р°РґРјС–РЅС–СЃС‚СЂР°С‚РѕСЂР° Р°Р±Рѕ РїРµСЂРµРІС–СЂС‚Рµ РїС–РґРєР»СЋС‡РµРЅРЅСЏ РґРѕ Р±Р°Р·Рё РґР°РЅРёС… С‡Рё РјРµСЂРµР¶С–.",
                "РљСЂРёС‚РёС‡РЅР° РїРѕРјРёР»РєР°",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                Log.Fatal(ex, "РљСЂРёС‚РёС‡РЅР° РЅРµРїРµСЂРµС…РѕРїР»РµРЅР° РїРѕРјРёР»РєР° РґРѕРјРµРЅСѓ Р·Р°СЃС‚РѕСЃСѓРЅРєСѓ (AppDomain.UnhandledException)");
            }
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            Log.Error(e.Exception, "РќРµРїРµСЂРµС…РѕРїР»РµРЅР° РїРѕРјРёР»РєР° С„РѕРЅРѕРІРѕС— Р·Р°РґР°С‡С– (TaskScheduler.UnobservedTaskException)");
            e.SetObserved();
        }
    }
}

