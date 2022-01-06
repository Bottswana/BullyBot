using System;
using Discord.Commands;
using System.Threading.Tasks;

namespace BullyBot.BotCommands
{
	[Group("debug")]
    public class TestBotCommand : ModuleBase<SocketCommandContext>
    {
        public TestBotCommand()
        {
            
        }

        /// <summary>
		/// Debug echo command
		/// </summary>
		/// <param name="text">Text to echo</param>
		[Command("echo")]
		[Summary("Returns the supplied text")]
		public async Task EchoAsync([Remainder][Summary("The text to return")] string text)
		{
			await Context.Channel.SendMessageAsync($"You said: {text}");
		}
    }
}