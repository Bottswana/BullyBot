using System;
using Serilog;
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
		
	    /// <summary>
	    /// Check the exercise the targetted user has performed today
	    /// </summary>
	    [Command("check")]
		[Summary("Check what exercise the user has done today")]
		public async Task CheckExercise([Summary("The user to target")] string User)
		{
			// Check we have a valid user string
			if( string.IsNullOrEmpty(User) )
			{
				await Context.Channel.SendMessageAsync($"Please specify a user for this command");
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
						var exerciseData = await TargetModule.DownloadData();
						await Context.Channel.SendMessageAsync(JsonConvert.SerializeObject(exerciseData));
						return;
					}
					catch( Exception Ex )
					{
						Log.Error(Ex, "Error retrieving exercise data");
						await Context.Channel.SendMessageAsync($"Sorry, I had a problem retrieving that data: {Ex.Message}");
						return;
					}
				}
				else
				{
					// Cant create instance
					Log.Error("Unable to create an instance with name {DataSource}", DataSource);
					await Context.Channel.SendMessageAsync($"Sorry, I had a problem creating an instance with name {DataSource}");
					return;
				}
			}

			// Unable to find the user
			await Context.Channel.SendMessageAsync($"User not found or exercise module not active for user: {User}");
		}
		
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