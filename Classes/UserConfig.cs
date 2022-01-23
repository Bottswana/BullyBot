using System;
using Serilog;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace BullyBot.Classes;
public static class UserConfig
{
    /// <summary>
    /// Retrieve all Usernames in the configuration
    /// </summary>
    /// <param name="AppConfig">Instance of the AppConfig</param>
    /// <returns>Array of users</returns>
    public static string[] GetConfigUsers(IConfiguration AppConfig)
    {
		var ReturnUsers = new List<string>();
	    try
	    {
		    var ModuleConfig = AppConfig.GetSection("BullyBot:BullyUsers").Get<IConfigurationSection[]>();
			foreach( var Section in ModuleConfig )
			{
				if( Section == null ) continue;
				var Username = Section.GetValue<string>("Name");
				if( !string.IsNullOrEmpty(Username) )
				{
					ReturnUsers.Add(Section.GetValue<string>("Name"));
				}
			}
	    }
	    catch( Exception Ex )
	    {
		    Log.Error(Ex, "Error retrieving config users");
	    }
	    
	    return ReturnUsers.ToArray();
    }
    
    /// <summary>
    /// Retrieve Module section for a user
    /// </summary>
    /// <param name="AppConfig">Instance of the AppConfig</param>
    /// <param name="User">Username to look for</param>
    /// <returns>Array of modules defined in the user config</returns>
    public static IConfigurationSection[] GetUserModules(IConfiguration AppConfig, string User)
    {
	    try
	    {
		    var ModuleConfig = AppConfig.GetSection("BullyBot:BullyUsers").Get<IConfigurationSection[]>();
			foreach( var Section in ModuleConfig )
			{
				if( Section == null ) continue;
				if( User.ToLower() == Section.GetValue<string>("Name").ToLower() )
				{
					return Section.GetSection("Modules").Get<IConfigurationSection[]>();
				}
			}
	    }
	    catch( Exception Ex )
	    {
		    Log.Error(Ex, "Error retrieving user configuration section");
	    }
	    
		return null;
    }
    
    /// <summary>
    /// Get the configuration for a specific module for a user
    /// </summary>
    /// <param name="AppConfig">Instance of the AppConfig</param>
    /// <param name="User">Username to look for</param>
    /// <param name="ModuleName">Module to get config for</param>
    /// <returns>Module config and DataSource, or null if not found</returns>
    public static (string DataSource, IConfiguration Config)? GetModule(IConfiguration AppConfig, string User, string ModuleName)
    {
	    try
	    {
			var UserModules = GetUserModules(AppConfig, User);
			if( UserModules != null )
			{
				foreach( var Module in UserModules )
				{
					if( Module == null ) continue;
					if( ModuleName.ToLower() == Module.GetValue<string>("Module").ToLower() )
					{
						var ModuleClass = Module.GetValue<string>("DataSource");
						var ModuleConfig = Module.GetSection("Config");
						if( !string.IsNullOrEmpty(ModuleClass) && ModuleConfig != null )
						{
							return (ModuleClass, ModuleConfig);
						}
					}
				}
			}
	    }
	    catch( Exception Ex )
	    {
		    Log.Error(Ex, "Error retrieving users module config section");
	    }
	    
		return null;
    }
}