using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace BullyBot.Classes
{
    public class DiscordBot
    {
        
        public DiscordBot(IConfigurationSection Config)
        {
            
        }
        
        internal async Task RunDiscordBot()
        {
            Log.Debug("Initialising Discord Bot");
            await Task.Delay(TimeSpan.FromSeconds(10));
            
            
            
        }
    }
}