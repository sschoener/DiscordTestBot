using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace DiscordTestBot
{
    public class LoggingService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly IConfigurationRoot _config;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _discordLogger;
        private readonly ILogger _commandsLogger;
        private readonly ILogger _debugLogger;

        public LoggingService(DiscordSocketClient discord,
                              CommandService commands,
                              IConfigurationRoot config,
                              ILoggerFactory loggerFactory)
        {
            _discord = discord;
            _commands = commands;
            _config = config;

            _loggerFactory = ConfigureLogging(loggerFactory);
            _debugLogger = _loggerFactory.CreateLogger("debug");
            _discordLogger = _loggerFactory.CreateLogger("discord");
            _commandsLogger = _loggerFactory.CreateLogger("commands");

            _discord.Log += LogDiscord;
            _commands.Log += LogCommand;
        }

        private ILoggerFactory ConfigureLogging(ILoggerFactory factory)
        {
            if (!System.Enum.TryParse<LogLevel>(_config["loglevel"], true, out var logLevel)) {
                logLevel = LogLevel.Debug;
            }            
            factory.AddConsole(logLevel);
            return factory;
        }

        public void Log(string msg) {
            _debugLogger.Log(LogLevel.Debug, 0, msg, null, null);
        }

        private Task LogDiscord(LogMessage message)
        {
            _discordLogger.Log(
                LogLevelFromSeverity(message.Severity), 
                0, 
                message,
                message.Exception, 
                (_1, _2) => message.ToString(prependTimestamp: false));
            return Task.CompletedTask;
        }

        private Task LogCommand(LogMessage message)
        {
            // Return an error message for async commands
            if (message.Exception is CommandException command)
            {
                // Don't risk blocking the logging task by awaiting a message send; ratelimits!?
                var _ = command.Context.Channel.SendMessageAsync($"Error: {command.Message}");
            }

            _commandsLogger.Log(
                LogLevelFromSeverity(message.Severity),
                0,
                message,
                message.Exception,
                (_1, _2) => message.ToString(prependTimestamp: false));
            return Task.CompletedTask;
        }

        private static LogLevel LogLevelFromSeverity(LogSeverity severity)
            => (LogLevel)(System.Math.Abs((int)severity - 5));
        
    }
}