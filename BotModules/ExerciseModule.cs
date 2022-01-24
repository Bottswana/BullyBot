using System;
using Serilog;
using Discord;
using Discord.Commands;
using BullyBot.Classes;
using Discord.WebSocket;
using System.Threading.Tasks;
using BullyBot.ExerciseDataSources;
using Microsoft.Extensions.Configuration;

namespace BullyBot.BotModules
{
	[Group("exercise")]
    public class ExerciseModule : ModuleBase<SocketCommandContext>
    {
		private readonly IConfiguration _Configuration;
		private readonly DiscordSocketClient _Client;
		
		#region Initialisation
		public ExerciseModule(IConfiguration appConfig, DiscordSocketClient client)
		{
			Program.OnNotificationTime += _OnNotificationTime;
			_Configuration = appConfig;
			_Client = client;
		}
		#endregion Initialisation
		
		#region Scheduled Timer
		private async void _OnNotificationTime()
		{
			// Get users and notification channel
			var ChannelSnowflake = _Configuration.GetValue<ulong>("BullyBot:NotificationChannelSnowflake");
	        var Users = UserConfig.GetConfigUsers(_Configuration);
	        if( ChannelSnowflake <= 0 )
	        {
		        Log.Error("Invalid notification SnowflakeId in config");
		        return;
	        }
	        
	        // Iterate over active users
	        Log.Debug("Running scheduled timer for ExerciseModule");
	        foreach( var ThisUser in Users )
	        {
				Log.Debug("Checking {User} against their goal", ThisUser);
				try
				{
					// Find the user configuration and download the data
					var (DataSource, ModuleConfig) = UserConfig.GetModule(_Configuration, ThisUser, "exercise") ?? (null, null);
					if( string.IsNullOrEmpty(DataSource) ) continue; // Not enabled for this user

					var TargetModule = _GetExerciseModule(DataSource, ModuleConfig);
					if( TargetModule == null ) throw new Exception("Unable to create DataSource");
					
					var ExerciseData = await TargetModule.DownloadData();
					if( ExerciseData == null ) throw new Exception("No data returned from DataSource");

					// Decide if we need to send an alert to the channel
					var UserAlert = ExerciseData.activeMinutes switch
					{
						< 30 and > 15 => new EmbedBuilder
						{
							Description = $"Hey <@!{UserConfig.GetConfigUserSnowflake(_Configuration, ThisUser)}>\nYou're nearly at the 30 minute goal, time to do a bit more exercise?",
							Title = "Exercise Goal",
							Color = Color.Orange
						},
						< 30 and > 0 => new EmbedBuilder
						{
							Description = $"Hey <@!{UserConfig.GetConfigUserSnowflake(_Configuration, ThisUser)}>\nYou've got a bit to go to meet the goal, time to go work out?",
							Title = "Exercise Goal",
							Color = Color.Red
						},
						0 => new EmbedBuilder
						{
							Description = $"Hey <@!{UserConfig.GetConfigUserSnowflake(_Configuration, ThisUser)}>\nDon't be a lazy bones! Time to go work out",
							Title = "Exercise Goal",
							Color = Color.Red
						},
						null => new EmbedBuilder
						{
							Description = $"Hey <@!{UserConfig.GetConfigUserSnowflake(_Configuration, ThisUser)}>\nDon't be a lazy bones! Time to go work out",
							Title = "Exercise Goal",
							Color = Color.Red
						},
						_ => null
					};
					
					// Check data health
					var StartOfToday = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);
					var TodayStart = Epoch.ToUnix(StartOfToday);
					if( ExerciseData.uploadDate < TodayStart )
					{
						UserAlert = new EmbedBuilder
						{
							Description = $"Hey <@!{UserConfig.GetConfigUserSnowflake(_Configuration, ThisUser)}>\nYour data is out of date! Here's the last health data I have for you",
							Title = "Exercise Goal",
							Color = Color.Red
						};
					}

					// If we decided to send an alert, populate base data and send
					if( UserAlert != null )
					{
						// Populate users health data
						UserAlert.AddField("Exercise Minutes", ExerciseData.activeMinutes != null ? $"{Math.Floor(ExerciseData.activeMinutes.Value)} minutes today" : "No Data Yet");
						UserAlert.AddField("Resting Heartrate", ExerciseData.restingHeartRate != null ? $"{Math.Floor(ExerciseData.restingHeartRate.Value)} bpm" : "No Data Yet");
						UserAlert.AddField("Step Count", ExerciseData.numberSteps != null ? $"{Math.Floor(ExerciseData.numberSteps.Value)} steps" : "No Data Yet");
						
						// Format date from Unix Timestamp
						var DataTime = Epoch.FromUnix(ExerciseData.uploadDate);
						if( DataTime != null)
						{
							var TimeFormat = DataTime?.ToString("dd/MM/yyyy HH:mm:ss");
							UserAlert.WithFooter(footer => footer.Text = $"Data retrieved @ {TimeFormat}");
						}
					
						// Send to the channel
						var NotificationChannel = (IMessageChannel) await _Client.GetChannelAsync(ChannelSnowflake);
						await NotificationChannel.SendMessageAsync(null, false, UserAlert.Build());
					}
				}
				catch( Exception Ex )
				{
					Log.Error(Ex, "Error checking excersise for user {ThisUser}", ThisUser);
				}
	        }
		}
		#endregion

		#region Commands
		[Command]
		[Summary("Help Command")]
		public async Task Help()
		{
			// Setup response object
		    var CommandResponse = new EmbedBuilder
	        {
				Description = $"This module encourages good exercise!",
				Title = "Exercise Module",
				Color = Color.Green
	        };
	        
	        // Setup available commands
	        CommandResponse.AddField("exercise list", "List users that have this module enabled");
	        CommandResponse.AddField("exercise check username", "Check what exercise listed users have performed today");
	        await ReplyAsync(embed: CommandResponse.Build());
		}
		
	    /// <summary>
	    /// Check the exercise the targetted user has performed today
	    /// </summary>
	    [Command("list")]
		[Summary("Check what users have this module enabled")]
		public async Task ListUsers()
		{
			// Setup response object
		    var CommandResponse = new EmbedBuilder
	        {
				Description = $"These are the users enabled for this module",
				Title = "Exercise Module Users",
				Color = Color.Green
	        };
		    
	        var Users = UserConfig.GetConfigUsers(_Configuration);
	        foreach( var ThisUser in Users )
	        {
		        CommandResponse.AddField($"Username: {ThisUser}", $"Discord Tag: <@!{UserConfig.GetConfigUserSnowflake(_Configuration, ThisUser)}>");
	        }
	        
			// Return user list
			await ReplyAsync(embed: CommandResponse.Build());
		}
		
	    /// <summary>
	    /// Check the exercise the targetted user has performed today
	    /// </summary>
	    [Command("check")]
		[Summary("Check what exercise the user has done today")]
		public async Task CheckExercise([Summary("The user to target")] string User)
		{
			// Setup response object
		    var CommandResponse = new EmbedBuilder
	        {
				Description = $"Here is the latest exercise data I have for <@!{UserConfig.GetConfigUserSnowflake(_Configuration, User)}>",
				Title = "Check Exercise Data",
				Color = Color.Green
	        };

		    // Check we have a valid user string
			if( string.IsNullOrEmpty(User) )
			{
				CommandResponse.WithColor(Color.Blue);
				CommandResponse.AddField("Command Error", "Please specify a user with this command");
				await ReplyAsync(embed: CommandResponse.Build());
				return;
			}

			// See if we can get a configuration for this user
			var (DataSource, ModuleConfig) = UserConfig.GetModule(_Configuration, User, "exercise") ?? (null, null);
			if( !string.IsNullOrEmpty(DataSource) )
			{
				Log.Debug("Found config for user {User}, Initiating DataSource {Source}", User, DataSource);
				var TargetModule = _GetExerciseModule(DataSource, ModuleConfig);
				if( TargetModule != null )
				{
					try
					{
						// Fetch the current data for this user using the DataSource in the configuration
						var ExerciseData = await TargetModule.DownloadData();
						if( ExerciseData == null )
						{
							CommandResponse.AddField("Execution Error", "Sorry, I couldn't execute this command");
							CommandResponse.AddField("Error Message", "No data was returned by DataSource");
							CommandResponse.WithColor(Color.Red);
							await ReplyAsync(embed: CommandResponse.Build());
							return;
						}
						
						// Format the response to the user
						CommandResponse.AddField("Exercise Minutes", ExerciseData.activeMinutes != null ? $"{Math.Floor(ExerciseData.activeMinutes.Value)} minutes today" : "No Data Yet");
						CommandResponse.AddField("Resting Heartrate", ExerciseData.restingHeartRate != null ? $"{Math.Floor(ExerciseData.restingHeartRate.Value)} bpm" : "No Data Yet");
						CommandResponse.AddField("Step Count", ExerciseData.numberSteps != null ? $"{Math.Floor(ExerciseData.numberSteps.Value)} steps" : "No Data Yet");
						
						// Format date from Unix Timestamp
						var DataTime = Epoch.FromUnix(ExerciseData.uploadDate);
						if( DataTime != null)
						{
							var TimeFormat = DataTime?.ToString("dd/MM/yyyy HH:mm:ss");
							CommandResponse.WithFooter(footer => footer.Text = $"Data retrieved @ {TimeFormat}");
						}
						
						// Respond to the command
						await ReplyAsync(embed: CommandResponse.Build());
						return;
					}
					catch( Exception Ex )
					{
						// Error executing instance
						Log.Error(Ex, "Error retrieving exercise data");
						CommandResponse.AddField("Execution Error", "Sorry, I couldn't execute this command");
						CommandResponse.AddField("Error Message", Ex.Message);
						CommandResponse.WithColor(Color.Red);
						await ReplyAsync(embed: CommandResponse.Build());
						return;
					}
				}
				else
				{
					// Cant create instance
					Log.Error("Unable to create an instance with name {DataSource}", DataSource);
					CommandResponse.AddField("Execution Error", "Sorry, I couldn't execute this command");
					CommandResponse.AddField("Error Message", $"No such class with name {DataSource}");
					CommandResponse.WithColor(Color.Red);
					await ReplyAsync(embed: CommandResponse.Build());
					return;
				}
			}

			// Unable to find the user
			CommandResponse.WithColor(Color.Blue);
			CommandResponse.AddField("Command Error", "User not found or `exersise` isn't active for this user");
			await ReplyAsync(embed: CommandResponse.Build());
		}
		#endregion Commands
		
		#region Private Methods
		/// <summary>
		/// Create an instance of the defined DataSource
		/// </summary>
		/// <param name="DataSourceClass">Class Name</param>
		/// <param name="Config">Config for initialiser</param>
		/// <returns>Class implementing IExerciseDataSource</returns>
		private static IExerciseDataSource _GetExerciseModule(string DataSourceClass, IConfiguration Config)
		{
			try
			{
				// Find class by module name
				var ClassType = Type.GetType(DataSourceClass);
				if( ClassType != null )
				{
					return (IExerciseDataSource)Activator.CreateInstance(ClassType, Config);
				}	
			}
			catch( Exception ) {}
			return null;
		}
		#endregion Private Methods
    }
}