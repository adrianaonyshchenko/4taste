using Microsoft.Extensions.Configuration;
using Serilog;
using System.IO;
using System.Windows;

namespace FourTaste.WPF
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

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
            services.AddSingleton<ILogger>(_ => Log.Logger);

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql("Host=ep-mute-sea-b5ks3hrm-pooler.c-7.us-east-2.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_a8u2btDTpHiX;Ssl Mode=Require;Trust Server Certificate=true"));

            services.AddScoped<IRepository<Allergy>, AllergyRepository>();
            services.AddScoped<IAllergyService, AllergyService>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<UserService>();

            services.AddTransient<ManageAllergiesWindow>();
            services.AddTransient<RegisterWindow>();
            services.AddTransient<MainWindow>();
        }
    }
}