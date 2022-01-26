using System;
using Serilog;
using System.Threading;
using BullyBot.Classes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BullyBot
{
    public delegate void OnNotifyTime(int TriggerHour);
    
    internal class Program
    {
        /// <summary>
        /// Classes which are available via Dependancy Injection in Commands
        /// </summary>
        internal static IServiceProvider InjectionClasses { get; private set; }
        
        /// <summary>
        /// Event triggers when the notification time in the configuration is reached
        /// </summary>
        internal static event OnNotifyTime OnNotificationTime;

        /// <summary>
        /// Application Entry Point
        /// </summary>
        /// <param name="args">Optional command line arguments</param>
        public static void Main(string[] args)
        {
            // Attempt to load the configuration file, and exit if we cannot find it
            var ApplicationConfigFile = InitialisationHandler.LoadConfiguration(args);
            if( ApplicationConfigFile == null )
            {
                Console.WriteLine("Error: Configuration file is missing. Please ensure the application has access to appsettings.json either in the binary directory or the direct parent directory.");
                Console.WriteLine("If the configuration file cannot be discovered automatically, you can specify it manually with the --config command line parameter");
                Environment.Exit(2);
            }
            
            // Configure the logging service
            Log.Logger = InitialisationHandler.InitLogger(ApplicationConfigFile, "BullyBot");
            
            // Create discord service
            var Bot = new DiscordBot(ApplicationConfigFile.GetSection("BullyBot:DiscordConfig"));
            
            // Add classes for Dependancy Injection
            var Collection = new ServiceCollection();
                Collection.AddSingleton(ApplicationConfigFile);
                Collection.AddSingleton(Bot._DiscordClient);
                
            InjectionClasses = Collection.BuildServiceProvider();
            
            // Setup the once-per-hour timer (if notification is enabled
            if( !string.IsNullOrEmpty(ApplicationConfigFile.GetValue<string>("BullyBot:NotificationChannelSnowflake")) )
            {
                // Get the time interval between now and the next hour
                var TimeNow = DateTime.Now;
                var AlertTime = new TimeSpan(TimeNow.Hour + 1, 0, 0) - TimeNow.TimeOfDay;
                Log.Debug("Notification timer delay for next hour is {AlertTime}", AlertTime);

                // It is currently before the scheduled time to run
                var _ = new Timer(_ =>
                {
                    Log.Debug("Timer has reached on the hour - firing events");
                    OnNotificationTime?.Invoke(DateTime.Now.Hour);
                }, null, AlertTime, new TimeSpan(1, 0, 0));
            }

            // Initialise the main thread by handing control to the bot class
            try
            {
                var DiscordBotThread = Bot.RunDiscordBot();
                DiscordBotThread.Wait();
            }
            catch( Exception Ex )
            {
                Log.Fatal(Ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}