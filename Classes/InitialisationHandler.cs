using System;
using Serilog;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace BullyBot.Classes
{
    /// <summary>
    /// Helpers for executable initialisation
    /// </summary>
    public static class InitialisationHandler
    {
        /// <summary>
        /// Find the configuration file for the executable
        /// </summary>
        /// <param name="Arguments">Arguments parsed during launch</param>
        /// <returns>iConfigurationBuilder if valid, or null if not found</returns>
        public static IConfiguration LoadConfiguration(IEnumerable<string> Arguments)
        {
            string ApplicationConfigFile = null;
            
            // Configuration File Command Argument
            var ArgumentSearch = Arguments.FirstOrDefault(q => q.Contains("--config=", StringComparison.CurrentCultureIgnoreCase));
            if( !string.IsNullOrEmpty(ArgumentSearch) )
            {
                var ConfigurationFile = ArgumentSearch.Split('=')[1];
                if( ConfigurationFile.EndsWith(".json") && File.Exists(ConfigurationFile) )
                {
                    Console.WriteLine($"Configuration file loaded from path: '{ConfigurationFile}");
                    ApplicationConfigFile = ConfigurationFile;
                }
            }
            
            // Configuration File Discovery
            var ExecutablePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)?.Replace("file:\\", "").Replace("file:", "");
            if( ApplicationConfigFile == null && ExecutablePath != null )
            {
                // First check the parent directory for a global configuration file
                var GlobalAppConfig = Path.Combine(Directory.GetParent(ExecutablePath)?.FullName ?? "", "appsettings.json");
                if( File.Exists(GlobalAppConfig) )
                {
                    Console.WriteLine($"Configuration file loaded from path: '{GlobalAppConfig}");
                    ApplicationConfigFile = GlobalAppConfig;
                }
                
                // Finally check the current directory (standard asp.net core implementation)
                var ProjectAppConfig = Path.Combine(ExecutablePath, "appsettings.json");
                if( File.Exists(ProjectAppConfig) )
                {
                    Console.WriteLine($"Configuration file loaded from path: '{ProjectAppConfig}");
                    ApplicationConfigFile = ProjectAppConfig;
                }
            }
            
            // Return the configuration file path
            if( ApplicationConfigFile == null ) return null;
            return new ConfigurationBuilder().AddJsonFile(ApplicationConfigFile, false, true)
            .AddJsonFile(ApplicationConfigFile.Replace(".json", ".development.json"), true, true).Build();
        }
        
        /// <summary>
        /// Initialise an instance of the Logger using the configuration file
        /// </summary>
        /// <param name="Configuration">IConfigurationBuilder instance of the config</param>
        /// <param name="SectionName">The section of the config file to load from</param>
        /// <returns>Logging instance</returns>
        /// <exception cref="ArgumentException">Invalid Configuration File</exception>
        public static ILogger InitLogger(IConfiguration Configuration, string SectionName)
        {
            if( Configuration == null ) throw new ArgumentException("Invalid Configuration File");
            return new LoggerConfiguration().ReadFrom.Configuration(Configuration.GetSection(SectionName)).CreateLogger();
        }
    }
}