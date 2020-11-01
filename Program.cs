using AppSearcher.Interfaces;
using AppSearcher.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Threading.Tasks;

namespace AppSearcher
{
    class Program
    {
        static async Task Main()
        { 
            try
            {
                var serviceCollection = new ServiceCollection();
                ConfigureServices(serviceCollection);

                var serviceProvider = serviceCollection.BuildServiceProvider();

                await serviceProvider.GetService<Searcher>().RunAsync();
            }
            catch (Exception e)
            {
                Log.Logger.Error($"The error is ocuerred {e.Message}");
                throw;
            }
        }

        private static void ConfigureServices(IServiceCollection serviceCollection)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true)
                .AddEnvironmentVariables()
            .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            serviceCollection.AddLogging(configure => configure.AddSerilog())
                .Configure<SearchParameters>(configuration.GetSection("SearchParameters"))
                .AddTransient<Searcher>()
                .AddTransient<ISearchService, SearchService>()
                .AddHttpClient();
        }
    }
}
