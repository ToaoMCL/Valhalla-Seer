using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Interactivity;
using System.Threading.Tasks;

namespace Valhalla_Seer.Commands
{
    public class FunCommands
    {
        [Command("ping")]
        [Description("Returns pong, use to test if the bot is working")]
        public async Task Ping(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync("Pong").ConfigureAwait(false);
        }


        [Command("add")]
        [Description("Adds two numbers together")]
        public async Task Add(CommandContext ctx, int n1, int n2)
        {
            await ctx.Channel.SendMessageAsync((n1 + n2).ToString()).ConfigureAwait(false);
        }

        [Command("response")]
        public async Task Response(CommandContext ctx)
        {
            var interactivity = ctx.Client.GetInteractivityModule();
            var message = await interactivity.WaitForMessageAsync(x => x.Channel == ctx.Channel);
            await ctx.Channel.SendMessageAsync(message.Message.Content);
        }


    }
}
