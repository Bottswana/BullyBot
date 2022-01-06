using System;
using Serilog;
using Discord;
using System.Threading;
using Discord.Commands;
using Discord.WebSocket;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace BullyBot.Classes
{
    public class DiscordBot
    {
        private readonly DiscordSocketClient _DiscordClient;
        private readonly CommandService _DiscordCommand;
        private readonly string _BotKey;

        #region Initialisation
        public DiscordBot(IConfiguration Config)
        {
            // Configure Discord.Net client
            _DiscordClient = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.GuildMessages | GatewayIntents.Guilds,
                LogLevel = LogSeverity.Verbose
            });
        
            _DiscordCommand = new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Verbose,
                CaseSensitiveCommands = false
            });
            
            // Configure log handlers
            _DiscordCommand.Log += DiscordOnLog;
            _DiscordClient.Log += DiscordOnLog;

            // Retrieve the bot key from the configuration
            var BotString = Config?.GetValue<string>("BotToken");
            if( string.IsNullOrEmpty(BotString) )
            {
                throw new Exception("Bot Token is invalid or missing from the configuration");
            }
            
            _BotKey = BotString;
        }
        #endregion Initialisation

        #region Internal Methods
        internal async Task RunDiscordBot()
        {
            // Configure command handler
            await _DiscordCommand.AddModulesAsync(Assembly.GetEntryAssembly(), Program.InjectionClasses);
            _DiscordClient.MessageReceived += DiscordClientOnMessageReceived;
            
            // Connect to the Discord Gateway
            Log.Information("Connecting to Discord Gateway");
            await _DiscordClient.LoginAsync(TokenType.Bot, _BotKey);
            await _DiscordClient.StartAsync();

            // Wait infinitely so your bot actually stays connected.
            await Task.Delay(Timeout.Infinite);
        }
        #endregion Internal Methods

        #region Private Methods
        private async Task DiscordClientOnMessageReceived(SocketMessage arg)
        {
            var OffsetPos = 0;
            
            // Ignore messages that are not for us
            if ( arg is not SocketUserMessage msg ) return;
            if ( msg.Author.Id == _DiscordClient.CurrentUser.Id || !msg.HasMentionPrefix(_DiscordClient.CurrentUser, ref OffsetPos) || msg.Author.IsBot ) return;
            
            try
            {
                // Create a Command Context.
                var CommandContext = new SocketCommandContext(_DiscordClient, msg);
                var CommandResult = await _DiscordCommand.ExecuteAsync(CommandContext, OffsetPos, Program.InjectionClasses);
                if( !CommandResult.IsSuccess )
                {
                    Log.Error("Failed to execute command ({Command}) from '{User}': {Error}", msg.CleanContent, msg.Source, CommandResult.ErrorReason);
                }
            }
            catch( Exception Ex )
            {
                Log.Error(Ex, "Failed to execute command ({Command}) from '{User}'", msg.CleanContent, msg.Source);
            }
        }

        private Task DiscordOnLog(LogMessage arg)
        {
            Log.Debug(arg.Message);
            return null;
        }
        #endregion Private Methods
    }
}

