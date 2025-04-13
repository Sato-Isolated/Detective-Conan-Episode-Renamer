using System;
using System.IO;
using System.Threading.Tasks;
using DetectiveConanRenamer.Interfaces;
using DetectiveConanRenamer.Services;
using DetectiveConanRenamer.Utils;
using Spectre.Console;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using DetectiveConanRenamer.Models;

namespace DetectiveConanRenamer
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                var services = ConfigureServices();
                var serviceProvider = services.BuildServiceProvider();

                var menuService = serviceProvider.GetRequiredService<IMenuService>();
                await menuService.ShowMainMenu();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Une erreur fatale est survenue : {ex.Message}");
                Console.WriteLine("Appuyez sur une touche pour quitter...");
                Console.ReadKey();
            }
        }

        private static IServiceCollection ConfigureServices()
        {
            var services = new ServiceCollection();

            // Services
            services.AddSingleton<ILoggingService, LoggingService>();
            services.AddSingleton<IValidationService, ValidationService>();
            services.AddSingleton<IMenuService, MenuService>();
            services.AddSingleton<IEpisodeService, EpisodeService>();
            services.AddSingleton<IFileRenamer, FileRenamerService>();
            services.AddSingleton<IWikiScraperService, WikiScraperService>();
            services.AddSingleton<IBackupService, BackupService>();
            services.AddSingleton<IRegexPatternService, RegexPatternService>();

            return services;
        }
    }
}
