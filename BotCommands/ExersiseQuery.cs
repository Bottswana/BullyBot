using System;
using Serilog;
using Discord;
using Discord.Commands;
using BullyBot.Classes;
using System.Threading.Tasks;
using BullyBot.ExerciseDataSources;
using Microsoft.Extensions.Configuration;

namespace BullyBot.BotCommands
{
	[Group("exercise")]
    public class ExerciseQuery : ModuleBase<SocketCommandContext>
    {
		private readonly IConfiguration _Configuration;
		
		#region Initialisation
		public ExerciseQuery(IConfiguration appConfig)
		{
			_Configuration = appConfig;
		}
		#endregion Initialisation
		
		#region Commands
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
						
						// Format the response to the user
						CommandResponse.AddField("Exercise Minutes", ExerciseData.activeMinutes != null ? $"{ExerciseData.activeMinutes} minutes today" : "No Data Yet");
						CommandResponse.AddField("Resting Heartrate", ExerciseData.restingHeartRate != null ? $"{ExerciseData.restingHeartRate} bpm" : "No Data Yet");
						CommandResponse.AddField("Step Count", ExerciseData.numberSteps != null ? $"{ExerciseData.numberSteps} steps" : "No Data Yet");
						
						// Format date from Unix Timestamp
						var DataTime = Epoch.FromUnix(ExerciseData.uploadDate);
						if( DataTime != null)
						{
							var TimeFormat = DataTime?.ToString("MM/dd/yyyy HH:mm:ss");
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