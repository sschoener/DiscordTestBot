using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordTestBot
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; }

        private Startup(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("_configuration.json");
            Configuration = builder.Build();
        }

        public static async Task RunAsync(string[] args)
        {
            var startup = new Startup(args);
            await startup.RunAsync();
        }

        public async Task RunAsync()
        {
            var provider = ConfigureServices();

            // these two lines are only here for their side effects; they force the services
            // into existence
            provider.GetRequiredService<CommandHandlingService>();
            provider.GetRequiredService<LoggingService>();
            provider.GetRequiredService<MongoDBService>().Start();

            // initialize the connection to Discord
            await provider.GetRequiredService<StartupService>().StartAsync();


            // wait indefinitely so your bot doesn't disconnect
            await Task.Delay(-1);
        }

        private IServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
            .AddLogging()
            .AddSingleton<LoggingService>()
            .AddSingleton<StartupService>()
            .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
                // cache 1000 messages per channel
                MessageCacheSize = 1000
            }))
            .AddSingleton(new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Verbose,
                DefaultRunMode = RunMode.Async,
                CaseSensitiveCommands = false
            }))
            .AddSingleton<CommandHandlingService>()
            .AddSingleton<MongoDBService>()
            .AddSingleton<UserScoreService>()
            .AddSingleton(Configuration)
            .BuildServiceProvider();
        }
    }
}