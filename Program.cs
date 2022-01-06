using System;
using Serilog;
using BullyBot.Classes;
using Microsoft.Extensions.DependencyInjection;

namespace BullyBot
{
    internal class Program
    {
        /// <summary>
        /// Classes which are available via Dependancy Injection in Commands
        /// </summary>
        internal static readonly IServiceProvider InjectionClasses;
        
        /// <summary>
        /// Initialise Dependancy Injection
        /// </summary>
        static Program()
        {
            var Collection = new ServiceCollection();
            InjectionClasses = Collection.BuildServiceProvider();
        }
        
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
            
            // Initialise the main thread by handing control to the bot class
            try
            {
                Log.Logger = InitialisationHandler.InitLogger(ApplicationConfigFile, "BullyBot");
                var DiscordBotThread = new DiscordBot(ApplicationConfigFile.GetSection("BullyBot:DiscordConfig")).RunDiscordBot();
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