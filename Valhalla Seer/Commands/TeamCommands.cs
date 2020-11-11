using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
//using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Valhalla_Seer.Commands
{
    public class TeamCommands 
    {



        [Command("join")]
        public async Task Join(CommandContext ctx)
        {
            var joinEmbed = new DiscordEmbedBuilder
            {
                Title = "Would you like to join?",
                ThumbnailUrl = ctx.Client.CurrentUser.AvatarUrl,
                Color = DiscordColor.Green
            };

            var joinMessage =  await ctx.Channel.SendMessageAsync(embed: joinEmbed).ConfigureAwait(false);

            var thumbsUpEmoji = DiscordEmoji.FromName(ctx.Client, ":+1:");
            var thumbsDownEmoji = DiscordEmoji.FromName(ctx.Client, ":-1:");
            await Task.Delay(1000);
            await joinMessage.CreateReactionAsync(thumbsUpEmoji).ConfigureAwait(false);
            await Task.Delay(1000);
            await joinMessage.CreateReactionAsync(thumbsDownEmoji).ConfigureAwait(false);

            var interactivity = ctx.Client.GetInteractivityModule();

            //var reactionResult = await interactivity.WaitForReactionAsync(
            //    x => x.Name == thumbsUpEmoji ||
            //    x.Name == thumbsDownEmoji,
            //    joinMessage).ConfigureAwait(false));
            ////await interactivity.WaitForMessageReactionAsync();
            var reactionResult = await interactivity.WaitForMessageReactionAsync(
                x => x.Name == thumbsUpEmoji ||
                x.Name == thumbsDownEmoji,
                joinMessage).ConfigureAwait(false);

            var role = ctx.Guild.GetRole(771403730825904128);
            if (reactionResult.Emoji == thumbsUpEmoji)
            {
                await ctx.Member.GrantRoleAsync(role).ConfigureAwait(false) ;
            }
            else if(reactionResult.Emoji == thumbsDownEmoji)
            {
                await ctx.Member.RevokeRoleAsync(role).ConfigureAwait(false);
            }
            else
            {

            }

            await joinMessage.DeleteAsync().ConfigureAwait(false);


        }



    }
}
