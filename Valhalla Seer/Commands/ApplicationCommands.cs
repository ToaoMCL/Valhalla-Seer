using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using System.Threading.Tasks;
using System.Collections.Generic;
using Valhalla_Seer.DataStructures;
using Valhalla_Seer.Saving;
using System;
using Newtonsoft.Json;
using System.IO;
using System.Text;

namespace Valhalla_Seer.Commands
{

    /// <summary>
    /// 
    /// Saving should be moved into individual files and they should be removed from lists when un necassary 
    /// as everytime it gets saved again it often saved files that have not been modified    
    /// This would require a restructure of the saving system and the loading in this script
    /// 
    /// </summary>

    public class ApplicationCommands
    {
        // Save File Names
        const string
            APPLICATION_FOLDER = "Applications",
            PENDING_FOLDER = "Pending Applications",
            FINISHED_FOLDER = "Finished Applications",
            REACTION_FOLDER = "ReactionPosts",
            GUILD_DATA_FOLDER = "Guild Data",
            APPLICATION_EXTENTION = ".aplc",
            PENDING_APPLICATION_EXTENTION = ".paplc",
            FINISHED_APPLICATION_EXTENTION = ".faplc",
            REACTION_POST_EXTENTION = ".raplc",
            FINISHED_CHANNEL_ID = "FinishedChannelID.guild",
            PENDING_CHANNEL_ID = "PendingChannelID.guild",
            ROLE_IDS = "RoleIds.guild",
            AUTO_SAVE_DATA = "AutoSaveData.aplc";


        


        static DiscordEmoji
            thumbsUpEmoji,
            thumbsDownEmoji,
            questionMarkEmoji,
            tickBoxEmoji,
            xCrossEmoji,
            arrowLeft,
            arrowRight;

        static ulong recruitmentTeam = 0;
        static ulong pendingChannel = 0;
        static ulong finishedChannel = 0;

        static readonly List<Application> Applications = new List<Application>();
        static readonly List<ApplicationInProgress> ApplicationsInProgress = new List<ApplicationInProgress>();
        static readonly List<PendingApplication> PendingApplications = new List<PendingApplication>();
        static readonly List<ReactionPost> ReactionPosts = new List<ReactionPost>();
        static readonly List<InteractableFinishedApplication> InteractableFinishedApplications = new List<InteractableFinishedApplication>();
        static readonly List<ulong> activeApplicantIds = new List<ulong>();

        static bool InitRan = false; // Just to make sure it's not ran a second time but I should look for a better way of handling this
        static AutoSaveData AutoSaveData;
        static bool AutoSaving = false;
        static bool AutoSaveEnabled = false;

        public static async Task Init()
        {
            if (InitRan) return;
            // CheckForOldSaveData();
            GetEmojiRefrences();
            await LoadSaveData().ConfigureAwait(false);
            if (!AutoSaveData.AutoSaveOnStartUp) AutoSave();
            InitRan = true;
            Log("Init Done", ConsoleColor.Green);
        }

        private static void CheckForOldSaveData()
        {
            // Legacy Save File Names
            const string
                L_APPLICATIONS = "Applications.aplc",
                L_PENDING_APPLICATIONS = "PendingApplications.aplc",
                L_FINISHED_APPLICATIONS = "FinishedApplications.aplc",
                L_REACTION_POSTS = "ReactionPosts.aplc",
                L_CHANNEL_IDS = "ChannelIds.guild",
                L_ROLE_IDS = "RoleIds.guild",
                L_AUTO_SAVE_DATA = "AutoSaveData.aplc";

            Dictionary<string, object> data = (Dictionary<string, object>)SavingSystem.LoadFile("", L_APPLICATIONS);
        }

        private static async void AutoSave()
        {
            if (!AutoSaveEnabled || AutoSaving) return;
            AutoSaving = true;
            await Task.Delay(AutoSaveData.AutoSaveInterval);
            Save();
            AutoSaving = false;
            AutoSave();
        }
        private static void GetEmojiRefrences()
        {
            thumbsUpEmoji = DiscordEmoji.FromName(Bot.Client, ":+1:");
            thumbsDownEmoji = DiscordEmoji.FromName(Bot.Client, ":-1:");
            questionMarkEmoji = DiscordEmoji.FromName(Bot.Client, ":question:");
            tickBoxEmoji = DiscordEmoji.FromName(Bot.Client, ":white_check_mark:");
            xCrossEmoji = DiscordEmoji.FromName(Bot.Client, ":x:");
            arrowLeft = DiscordEmoji.FromName(Bot.Client, ":arrow_left:");
            arrowRight = DiscordEmoji.FromName(Bot.Client, ":arrow_right:");
        }
        static async Task LoadSaveData()
        {
            object pc = SavingSystem.LoadFile(GUILD_DATA_FOLDER, PENDING_CHANNEL_ID);
            object fc = SavingSystem.LoadFile(GUILD_DATA_FOLDER, FINISHED_CHANNEL_ID);
            object rt = SavingSystem.LoadFile(GUILD_DATA_FOLDER, ROLE_IDS);
            if (rt != null) recruitmentTeam = (ulong)rt;
            if (pc != null) pendingChannel = (ulong)pc;
            if (fc != null) finishedChannel = (ulong)fc;

            // LoadApplications must go first as the others will be searching for a application reference!
            await LoadApplications().ConfigureAwait(false);
            await LoadPendingApplications().ConfigureAwait(false);
            await LoadInteractableFinishedApplications().ConfigureAwait(false);
            await LoadReactionPosts().ConfigureAwait(false);
            LoadAutoSaveData();

            static async Task LoadApplications()
            {
                Log("Loading Application Forms", ConsoleColor.Yellow);
                List<object> loadedApplications = SavingSystem.LoadFileTypes(APPLICATION_FOLDER, APPLICATION_EXTENTION);
                foreach (object application in loadedApplications)
                {
                    Application loadedApplication = new Application("_LoadedApplication_");
                    await loadedApplication.RestoreState(application).ConfigureAwait(false);
                    Applications.Add(loadedApplication);
                    Log("Application " + loadedApplication.Title + " : Loaded", ConsoleColor.Green);
                }
            }
            static async Task LoadPendingApplications()
            {
                Log("Loading Finished Applications", ConsoleColor.Yellow);
                List<object> loadedFinishedApplications = SavingSystem.LoadFileTypes(PENDING_FOLDER, PENDING_APPLICATION_EXTENTION);
                foreach (object loadedFinishedApplication in loadedFinishedApplications)
                {
                    try
                    {
                        PendingApplication application = new PendingApplication(null, null, null, loadedFinishedApplication);
                        await application.RestoreState(loadedFinishedApplication).ConfigureAwait(false);
                        Log(
                            "Pending Application " + application.Message.Id +
                            " for " + application.Message.Channel.GuildId +
                            " in " + application.Message.ChannelId + " : Loaded", ConsoleColor.Green);
                        PendingApplications.Add(application);

                        AddPageScrollingInteractivity(application.Message, application.ApplicantRespones);
                        AddApplicationAproovalInteractivity(application);
                    }
                    catch
                    {
                        // currently only exists as save doesnt exist on rude exit
                        Log("Dead application found and skipped", ConsoleColor.Red);
                    }
                }
            }
            static async Task LoadReactionPosts()
            {
                Log("Loading ReactionPosts", ConsoleColor.Yellow);
                List<object> loadedReactionPosts = SavingSystem.LoadFileTypes(REACTION_FOLDER, REACTION_POST_EXTENTION);
                foreach (object loadedReactionPost in loadedReactionPosts)
                {
                    try
                    {
                        ReactionPost reactionPost = new ReactionPost(null, null);
                        await reactionPost.RestoreState(loadedReactionPost).ConfigureAwait(false);
                        if (reactionPost.Application_ == null)
                        {
                            Log("Reaction Post has missing application refrence, terminating interactivity and removing from list", ConsoleColor.Red);
                        }
                        else if (reactionPost.DiscordMessage_ == null)
                        {
                            Log("Reaction Post message has been deleted or is missing/cannot be found", ConsoleColor.Red);
                        }
                        else
                        {
                            Log("Reaction Post for " + reactionPost.Application_.Title + " loaded", ConsoleColor.Green);
                            // ReactionPosts.Add(reactionPost);
                            AddStartApplicationOnReactInteractivity(reactionPost);
                        }
                    }
                    catch
                    {
                        Log("Dead Reaction Post found and skipped", ConsoleColor.Red);
                    }
                }
            }
            static async Task LoadInteractableFinishedApplications()
            {
                Log("Loading Interactable Finished Applications", ConsoleColor.Yellow);
                List<object> saveData = SavingSystem.LoadFileTypes(FINISHED_FOLDER, FINISHED_APPLICATION_EXTENTION);
                foreach(object data in saveData)
                {
                    var ifa = new InteractableFinishedApplication(null, null);
                    await ifa.RestoreState(data).ConfigureAwait(false);
                    InteractableFinishedApplications.Add(ifa);
                    AddPageScrollingInteractivity(ifa.Message, ifa.Responses);
                }
            }
            static void LoadAutoSaveData()
            {
                Log("Checking for auto save data", ConsoleColor.Yellow);
                object saveData = SavingSystem.LoadFile(GUILD_DATA_FOLDER, AUTO_SAVE_DATA);
                if (saveData == null) AutoSaveData = new AutoSaveData(1800000, false);
                else AutoSaveData = (AutoSaveData)saveData;
                AutoSaveEnabled = AutoSaveData.AutoSaveOnStartUp;
            }
        } // <<----- State Restoration repeats itself I think it can be boiled into 1 method

        

        /// <summary>
        /// ###########
        /// 
        /// End Of Init
        /// 
        /// ###########
        /// </returns>


        private static string GetPageFooter(string[] list, int page)
        {
            page++;
            return "page " + page + "/" + list.Length;
        }
        private static async void ResumeApplicationInProgress(ApplicationInProgress applicationInProgress)
        {
            if (activeApplicantIds.Contains(applicationInProgress.Applicant.Id))
            {
                await applicationInProgress.Applicant.SendMessageAsync("You currently have a unfinished application").ConfigureAwait(false);
                return;
            }
            foreach(PendingApplication pending in PendingApplications)
            {
                if(pending.Title == applicationInProgress.Title && pending.Applicant == applicationInProgress.Applicant) 
                {
                    await applicationInProgress.Applicant.SendMessageAsync("You have a finished application of this type awaiting aprooval you may not start another.").ConfigureAwait(false);
                    return;
                }
            }
            var embed = new DiscordEmbedBuilder()
            {
                Title = "Instructions",
                Description = "You will be sent a series of questions that you can answer with either short or long answers. \n\n" +
                "Short questions can be answered by reacting with thumbs or or thumbs down, long answers will take any reaction and then listen to your next message.\n\n" +
                "Once you answer all questions the application will be complete and sent off for evaluation.",
                ThumbnailUrl = Bot.Client.CurrentUser.AvatarUrl,
                Color = DiscordColor.Purple
            };

            await applicationInProgress.Applicant.SendMessageAsync(embed:embed).ConfigureAwait(false);
            activeApplicantIds.Add(applicationInProgress.Applicant.Id);
            for (int i = (int)applicationInProgress.ResponseCount; i < applicationInProgress.Application_.Questions.Count; i++)
            {
                await SendQuestion(applicationInProgress.Application_.Questions[i], applicationInProgress).ConfigureAwait(false);
                await AddQuestionInteractivity(applicationInProgress).ConfigureAwait(false);
            }
            await applicationInProgress.Applicant.SendMessageAsync("Application Finished").ConfigureAwait(false);
            activeApplicantIds.Remove(applicationInProgress.Applicant.Id);

            // Application Aprooval
            // Description length limit 2048 char
            string[] results = applicationInProgress.GetApplicationResults();
            var applicationEmbed = new DiscordEmbedBuilder
            {
                Title = applicationInProgress.Title,
                Description = results[0],
                ThumbnailUrl = applicationInProgress.Applicant.AvatarUrl,
                Color = DiscordColor.Yellow
            };

            // await PostFinishedApplication().ConfigureAwait(false);
            DiscordMessage applicationMessage = await applicationInProgress.Guild.GetChannel(id: pendingChannel).SendMessageAsync(embed: applicationEmbed).ConfigureAwait(false);
            PendingApplication pendingApplication = new PendingApplication(applicationMessage, applicationInProgress.Applicant, applicationInProgress);
            PendingApplications.Add(pendingApplication);
            DiscordEmoji[] emojis = new DiscordEmoji[2] { thumbsUpEmoji, thumbsDownEmoji };
            await AddEmojis(pendingApplication.Message, emojis).ConfigureAwait(false);
            AddPageScrollingInteractivity(applicationMessage, results);
            AddApplicationAproovalInteractivity(pendingApplication);
        }
        private static async void AddPageScrollingInteractivity(DiscordMessage message, string[] list)
        {
            if (list.Length <= 1) return;
            if (message == null) return;
            if (list == null) return;
            await AddEmojis(message, new DiscordEmoji[2] { arrowLeft, arrowRight }).ConfigureAwait(false);    
            int page = 0;
            int previousPage;
            while (true)
            {
                var reactionResult = await WaitForReaction(message, new List<DiscordEmoji>() { arrowLeft, arrowRight } ).ConfigureAwait(false);
                previousPage = page;
                if (reactionResult == null) return;
                else if (reactionResult.Emoji == arrowLeft) page = Math.Clamp(page - 1, 0, list.Length - 1);
                else if (reactionResult.Emoji == arrowRight) page = Math.Clamp(page + 1, 0, list.Length - 1);
                await message.DeleteReactionAsync(reactionResult.Emoji, reactionResult.User).ConfigureAwait(false);
                if (previousPage == page) continue;

                var oldEmbed = message.Embeds[0];
                var newEmbed = new DiscordEmbedBuilder()
                {
                    Title = oldEmbed.Title,
                    Color = oldEmbed.Color,
                    ThumbnailUrl = oldEmbed.Thumbnail.Url.ToString(),
                    Description = list[page],

                };
                newEmbed.WithFooter(GetPageFooter(list, page));
                await message.ModifyAsync(embed: newEmbed).ConfigureAwait(false);
            }
        }
        private static async void AddApplicationAproovalInteractivity(PendingApplication application)
        {
            var applicationEmbed = new DiscordEmbedBuilder
            {
                Title = application.Title,
                Description = application.ApplicantRespones[0],
                ThumbnailUrl = application.Applicant.AvatarUrl,
                Color = DiscordColor.Yellow
            };
            applicationEmbed.WithFooter(GetPageFooter(application.ApplicantRespones, 0));
            
            var reactionResult = await WaitForReaction(application.Message, new List<DiscordEmoji>() { thumbsDownEmoji, thumbsUpEmoji }).ConfigureAwait(false);
            if (reactionResult == null) return;

            DiscordMember reactor = await application.Message.Channel.Guild.GetMemberAsync(reactionResult.User.Id).ConfigureAwait(false);
            
            //reactor.PermissionsIn(application.Message.Channel);
            //var a = reactor.Roles;
            //foreach(DiscordRole role in a)
            //{
            //    if (role.Id == recruitmentTeam) Log("has role", ConsoleColor.Blue);
            //    else Log("Doesn't have role", ConsoleColor.Blue);
            //}


            if (reactionResult.Emoji == thumbsUpEmoji)
            {
                await application.GrantRoles().ConfigureAwait(false);
                await application.Applicant.SendMessageAsync("Your application for " + applicationEmbed.Title + " has been approved").ConfigureAwait(false);
                applicationEmbed.Title = applicationEmbed.Title + "\n" + reactionResult.User.Username + " has approved this application:";
                applicationEmbed.Color = DiscordColor.Green;
            }
            else if (reactionResult.Emoji == thumbsDownEmoji)
            {
                await application.Applicant.SendMessageAsync("Your application for " + applicationEmbed.Title + " has been denied").ConfigureAwait(false);
                applicationEmbed.Title = applicationEmbed.Title + "\n" + reactionResult.User.Username + " has denied this application:";
                applicationEmbed.Color = DiscordColor.Red;
            }


            var message = await application.Message.Channel.Guild.GetChannel(id: finishedChannel).SendMessageAsync(embed: applicationEmbed).ConfigureAwait(false);
            AddPageScrollingInteractivity(message, application.ApplicantRespones);
            if (application.ApplicantRespones.Length > 1) InteractableFinishedApplications.Add(new InteractableFinishedApplication(message, application.ApplicantRespones));
            await Task.Delay(200);
            var channel = application.Message.Channel.Guild.GetChannel(pendingChannel);
            await channel.DeleteMessageAsync(application.Message).ConfigureAwait(false);
            PendingApplications.Remove(application);
        }
        private static async void AddStartApplicationOnReactInteractivity(ReactionPost post)
        {
            while (post.DiscordMessage_ != null)
            {
                var reactionResult = await WaitForReaction(post.DiscordMessage_).ConfigureAwait(false);
                if (reactionResult == null) return;
                DiscordMember member = await post.DiscordMessage_.Channel.Guild.GetMemberAsync(reactionResult.User.Id).ConfigureAwait(false);
                ApplicationInProgress newApplication = new ApplicationInProgress(member, reactionResult.Guild, post.Application_);
                ApplicationsInProgress.Add(newApplication);
                ResumeApplicationInProgress(newApplication);
            }
            Log("Message or user deleted stopping interactivity", ConsoleColor.Yellow);


        }
        public static Application GetApplication(string applicationAppliedFor)
        {
            foreach (Application app in Applications)
            {
                if (app.Title == applicationAppliedFor)
                {
                    return app;
                }
            }
            return null;
        }
        private async Task<Question> FindQuestion(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync("Type in application title").ConfigureAwait(false);         
            var message = await WaitForMessage(ctx.Channel).ConfigureAwait(false);
            if (message == null) return null;
            Application application = GetApplication(message.Message.Content);
            if (application == null)
            {
                await ctx.Channel.SendMessageAsync("Application not found.").ConfigureAwait(false);
                return null;
            }
            await ctx.Channel.SendMessageAsync("Type in question title").ConfigureAwait(false);
            await Task.Delay(100).ConfigureAwait(false);
            message = await WaitForMessage(ctx.Channel).ConfigureAwait(false);
            if (message == null) return null;
            return application.GetQuestion(message.Message.Content);
        }
        private static async Task AddEmojis(DiscordMessage applicationMessage, DiscordEmoji[] emojis)
        {
            foreach (DiscordEmoji emoji in emojis)
            {
                await applicationMessage.CreateReactionAsync(emoji).ConfigureAwait(false);
                await Task.Delay(200).ConfigureAwait(false);
            }
        }
        private static async Task AddQuestionInteractivity(ApplicationInProgress newApplication)
        {
            Question question = newApplication.Application_.Questions[(int)newApplication.ResponseCount];
            DiscordMessage message = newApplication.LastQuestionMessage;

            while (true)
            {
                if (question.ShortQuestion)
                {    
                    var reactionResult = await WaitForReaction(message, new List<DiscordEmoji>() { thumbsDownEmoji, thumbsUpEmoji, questionMarkEmoji } ).ConfigureAwait(false);
                    if (reactionResult == null) return;
                    if (reactionResult.Emoji == thumbsUpEmoji) newApplication.AddResponse(question, "yes");
                    else if (reactionResult.Emoji == thumbsDownEmoji) newApplication.AddResponse(question, "No");

                    if (reactionResult.Emoji != questionMarkEmoji) break;
                    await message.Channel.SendMessageAsync(question.Help).ConfigureAwait(false);
                }
                else
                {
                    var reactionResult = await WaitForReaction(message).ConfigureAwait(false);
                    if (reactionResult == null) return;
                    if (reactionResult.Emoji == questionMarkEmoji)
                    {
                        await message.Channel.SendMessageAsync(question.Help).ConfigureAwait(false);
                        continue;
                    }
                    else
                    {
                        await newApplication.Applicant.SendMessageAsync("Listening").ConfigureAwait(false);
                        var responseMessage = await WaitForMessage(message.Channel).ConfigureAwait(false);
                        newApplication.AddResponse(question, responseMessage.Message.Content);
                        break;
                    }

                }
            }
        } 
        private static async Task CheckIfChannelExists(CommandContext ctx, ulong channelId)
        {
            DiscordChannel channel = ctx.Guild.GetChannel(channelId);
            if (channel == null) await ctx.Channel.SendMessageAsync("Channel not found, Id invalid" + xCrossEmoji).ConfigureAwait(false);
            else await ctx.Channel.SendMessageAsync("Channel was succesfuly set" + tickBoxEmoji).ConfigureAwait(false);
        }
        private static async Task SendQuestion(Question question, ApplicationInProgress newApplication)
        {
            DiscordEmoji[] emojis = new DiscordEmoji[3] { thumbsUpEmoji, thumbsDownEmoji, questionMarkEmoji };

            var applicationEmbed = new DiscordEmbedBuilder
            {
                Title = question.Title,
                Description = question.Description,
                ThumbnailUrl = Bot.Client.CurrentUser.AvatarUrl,
                Color = DiscordColor.Green
            };

            var message = await newApplication.Applicant.SendMessageAsync(embed: applicationEmbed).ConfigureAwait(false);
            newApplication.SetLastQuestion(message);
            await AddEmojis(message, emojis).ConfigureAwait(false);
        }
        private static async Task<ReactionContext> WaitForReaction(DiscordMessage message, List<DiscordEmoji> emojis = null)
        {
            var interactivity = Bot.Client.GetInteractivityModule();
            ReactionContext reactionContext = null;
            while (reactionContext == null)
            {
                if (message == null) return null;
                if (emojis == null) reactionContext = await interactivity.WaitForMessageReactionAsync(message).ConfigureAwait(false);
                else reactionContext = await interactivity.WaitForMessageReactionAsync(x => emojis.Contains(x), message).ConfigureAwait(false);
            }
            return reactionContext;
        }
        private static async Task<MessageContext> WaitForMessage(DiscordChannel channel)
        {
            var interactivity = Bot.Client.GetInteractivityModule();
            MessageContext response = null;
            while (response == null)
            {
                if (channel == null) return null;
                response = await interactivity.WaitForMessageAsync(x => x.Channel == channel && x.Author != Bot.Client.CurrentUser).ConfigureAwait(false);
            }
            return response;
        }



        /// <summary>
        /// 
        /// COMMAND LIST
        /// 
        /// </returns>

        [Command("CleanUpApplications")]
        [RequireOwner]
        [Description("Removes the prefix from application titles")]
        public async Task CleanUpApplicationTitles()
        {
            var json = string.Empty;
            using (var fs = File.OpenRead("config.json"))
            {
                using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                {
                    json = await sr.ReadToEndAsync().ConfigureAwait(false);
                }
            }
            var configJson = JsonConvert.DeserializeObject<ConfigJson>(json);


            foreach (Application application in Applications) application.ChangeTitle(application.Title.Replace(configJson.Prefix, ""));
        }

        [Command("setup")]
        [RequireOwner]
        [Description("Gives a list of various tasks that have or hanven't been done for hte bot to function properly.")]
        public async Task SetUp(CommandContext ctx)
        {
            bool finished = true;

            var embed = new DiscordEmbedBuilder()
            {
                Title = "Setup List",
                Description =
                "Pending Application Channel :" + GetStatus(pendingChannel, ref finished) + "\n" +
                "Finished Application Channel :" + GetStatus(finishedChannel, ref finished) + "\n" +
                "Recruitment Role:" + GetRole(recruitmentTeam, ref finished) + "\n\n" +
                "Currently role checks on application confirmation occurs as a work around" +
                "make the pending and finished application channels private to the role " + "\n\n",
                ThumbnailUrl = ctx.Client.CurrentUser.AvatarUrl
            };



            if (finished)
            {
                embed.Color = DiscordColor.Green;
                embed.Description += "Setup Finished, the bot is ready. " + tickBoxEmoji;
            }
            else
            {
                embed.Color = DiscordColor.Red;
                embed.Description += "Setup Incomplete, the bot may not work. " + xCrossEmoji;
            }
            await ctx.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);


            string GetStatus(ulong channelID, ref bool finished)
            {
                DiscordChannel channel = ctx.Guild.GetChannel(channelID);
                if (channel == null)
                {
                    finished = false;
                    return "Channel not assigned " + xCrossEmoji;
                }
                else
                {
                    return "Channel found " + tickBoxEmoji;
                }
            }
            string GetRole(ulong reqruitmentRole, ref bool finished)
            {
                DiscordRole role = ctx.Guild.GetRole(reqruitmentRole);
                if (role == null)
                {
                    finished = false;
                    return "Reqruitment Team role not set " + xCrossEmoji;
                }
                else
                {
                    return "Reqruitment Team role found " + tickBoxEmoji;
                }
            }
        }

        [Command("as")]
        [RequireOwner]
        [Description("Starts or Stops the autosaving process, by default the bot will not autosave")]
        public async Task AutoSaveCommand(CommandContext ctx, [Description("Duration is calculated in milliseconds 1000 = 1 second, min 30 seconds")] uint saveInterval, bool autosave, [Description("Should the bot autosave by default when they start, type true for yes or false for no you can ignore this field if you dont want to change anything")] bool autosaveOnStartUp)
        {
            AutoSaveData.Update(saveInterval, autosaveOnStartUp);
            SaveAutoSaveData();
            Log("Autosave settings updated");
            AutoSaveEnabled = autosave;
            AutoSave();
            await Task.Delay(0);
        }

        [Command("crp")]
        [RequireOwner]
        [Description("Create a post that let's people start an application by reacting to it.")]
        public async Task CreateReactionPost(CommandContext ctx)
        {
            string title, description;

            // Check for application
            await ctx.Channel.SendMessageAsync("Type in application title").ConfigureAwait(false);
            var response = await WaitForMessage(ctx.Channel).ConfigureAwait(false);
            Application application = GetApplication(response.Message.Content);
            if (application == null)
            {
                await ctx.Channel.SendMessageAsync("Application not found.").ConfigureAwait(false);
                return;
            }
          
            // Set Message Title
            await ctx.Channel.SendMessageAsync("Message Title").ConfigureAwait(false);
            response = await WaitForMessage(ctx.Channel).ConfigureAwait(false);
            if (response == null) return;
            title = response.Message.Content;

            // Set Message Description
            await ctx.Channel.SendMessageAsync("Message Description").ConfigureAwait(false);
            response = await WaitForMessage(ctx.Channel).ConfigureAwait(false);
            if (response == null) return;
            description = response.Message.Content;


            var embed = new DiscordEmbedBuilder()
            {
                Title = title,
                Description = description,
                Color = DiscordColor.Aquamarine,
                ThumbnailUrl = ctx.Client.CurrentUser.AvatarUrl
            };

            DiscordMessage message = await ctx.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
            await AddEmojis(message, new DiscordEmoji[1] { tickBoxEmoji } );
            ReactionPost newPost = new ReactionPost(message, application);
            ReactionPosts.Add(newPost);
            AddStartApplicationOnReactInteractivity(newPost);
        }     

        [Command("SetPendingChannel")]
        [RequireOwner]
        [Description("Sets the pending channel where applications can be aprooved or denied.")]
        public async Task SetPendingChannel(CommandContext ctx, ulong channelId)
        {
            await CheckIfChannelExists(ctx, channelId).ConfigureAwait(false);
            pendingChannel = channelId;
        }

        [Command("SetFinishedChannel")]
        [RequireOwner]
        [Description("Sets the channel where aprooved or denied applications get posted.")]
        public async Task SetFinishedChannel(CommandContext ctx, ulong channelId)
        {
            await CheckIfChannelExists(ctx, channelId).ConfigureAwait(false);
            finishedChannel = channelId;
        }

        [Command("SetRecruitmentTeam")]
        [RequireOwner]
        [Description("Set the role of people who are authorised to accept applications.")]
        public async Task SetReqcruitmentTeamRole(CommandContext ctx, ulong roleID)
        {
            DiscordRole discordRole = ctx.Guild.GetRole(roleID);
            if(discordRole == null) await ctx.Channel.SendMessageAsync("Role was not found, Id invalid" + xCrossEmoji).ConfigureAwait(false);
            else await ctx.Channel.SendMessageAsync("Role was succesfully set" + tickBoxEmoji).ConfigureAwait(false);

            recruitmentTeam = roleID;
        }

        [Command("sa")]
        [Description("Start an application.")]
        public async Task StartApplication(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync("Type in application title").ConfigureAwait(false);
            var message = await WaitForMessage(ctx.Channel).ConfigureAwait(false);
            var application = GetApplication(message.Message.Content);
            if (application == null)
            {
                await ctx.Channel.SendMessageAsync("Application not found.").ConfigureAwait(false);
                return;
            }

            ApplicationInProgress applicationInProgress = new ApplicationInProgress(ctx.Member, ctx.Guild, application);
            ApplicationsInProgress.Add(applicationInProgress);
            ResumeApplicationInProgress(applicationInProgress);
        }

        [Command("ca")]
        [RequireOwner]
        [Description("Create a new application.")]
        public async Task CreateApplication(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync("Type in application title").ConfigureAwait(false);
            var response = await WaitForMessage(ctx.Channel).ConfigureAwait(false);
            if (response == null) return;

            Application application = GetApplication(response.Message.Content);
            if (application != null)
            {
                await ctx.Channel.SendMessageAsync("Application title already exists.").ConfigureAwait(false);
                return;
            }

            Applications.Add(new Application(response.Message.Content));
            await ctx.Channel.SendMessageAsync("New Application Created").ConfigureAwait(false);
        }

        [Command("cq")]
        [RequireOwner]
        [Description("Create a new question.")]
        public async Task CreateQuestion(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync("Type in application title").ConfigureAwait(false);
            var message = await  WaitForMessage(ctx.Channel).ConfigureAwait(false);
            if (message == null) return;

            Application application = GetApplication(message.Message.Content);
            if (application == null)
            {
                await ctx.Channel.SendMessageAsync("Application not found.").ConfigureAwait(false);
                return;
            }

            Question question = application.AddQuestion();

            await SetQuestionTitle(ctx, question).ConfigureAwait(false);
            await SetQuestionDescription(ctx, question).ConfigureAwait(false);
            await SetQuestionHelp(ctx, question).ConfigureAwait(false);
            await ctx.Channel.SendMessageAsync("Question Added").ConfigureAwait(false);
        }

        [Command("rq")]
        [RequireOwner]
        [Description("Remove a question")]
        public async Task RemoveQuestion(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync("Type in application title").ConfigureAwait(false);
            var message = await WaitForMessage(ctx.Channel).ConfigureAwait(false);
            if (message == null) return;
            Application application = GetApplication(message.Message.Content);
            if (application == null)
            {
                await ctx.Channel.SendMessageAsync("Application not found.").ConfigureAwait(false);
                return;
            }

            await ctx.Channel.SendMessageAsync("Type in question title").ConfigureAwait(false);
            message = await WaitForMessage(ctx.Channel).ConfigureAwait(false);
            if (message == null) return;
            Question question = application.GetQuestion(message.Message.Content);
            if (question == null)
            {
                await ctx.Channel.SendMessageAsync("Question not found").ConfigureAwait(false);
                return;
            }

            application.RemoveQuestion(question);
            await ctx.Channel.SendMessageAsync("Question removed").ConfigureAwait(false);
        }

        [Command("arta")]
        [RequireOwner]
        [Description("Adds a role to an application, if it is approved all roles given to this application will be granted to the applicant.")]
        public async Task AddRoleToApplication(CommandContext ctx, ulong roleId)
        {
            await ctx.Channel.SendMessageAsync("Type in application title").ConfigureAwait(false);
            var message = await WaitForMessage(ctx.Channel).ConfigureAwait(false);
            if (message == null) return;
            Application application = GetApplication(message.Message.Content);
            if (application == null)
            {
                await ctx.Channel.SendMessageAsync("Application not found.").ConfigureAwait(false);
                return;
            }

            application.AddRole(ctx, roleId);
            await ctx.Channel.SendMessageAsync("Role Added").ConfigureAwait(false);
        }

        [Command("at")]
        [RequireOwner]
        [Description("Change the title of an application")]
        public async Task ChangeApplicationTitle(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync("Provide current application title").ConfigureAwait(false);
            var messageContext = await WaitForMessage(ctx.Channel).ConfigureAwait(false);
            var application = GetApplication(messageContext.Message.Content);
            if (application == null) return;
            await ctx.Channel.SendMessageAsync("Provide new application title").ConfigureAwait(false);
            messageContext = await WaitForMessage(ctx.Channel).ConfigureAwait(false);
            application.ChangeTitle(messageContext.Message.Content);
        }

        [Command("rrfa")]
        [RequireOwner]
        [Description("Remove a role from an application")]
        public async Task RemoveRoleFromApplication(CommandContext ctx, ulong roleId)
        {
            await ctx.Channel.SendMessageAsync("Provide current application title").ConfigureAwait(false);
            var messageContext = await WaitForMessage(ctx.Channel).ConfigureAwait(false);
            var application = GetApplication(messageContext.Message.Content);
            if (application == null) return;
            application.RemoveRole(roleId);
        }

        [Command("qt")]
        [RequireOwner]
        [Description("Changes a question's title.")]
        public async Task ChangeQuestionTitle(CommandContext ctx)
        {

            await ctx.Channel.SendMessageAsync("Type in application title").ConfigureAwait(false);
            var message = await WaitForMessage(ctx.Channel).ConfigureAwait(false);
            if (message == null) return;
            Application application = GetApplication(message.Message.Content);
            if (application == null)
            {
                await ctx.Channel.SendMessageAsync("Application not found.").ConfigureAwait(false);
                return;
            }


            await ctx.Channel.SendMessageAsync("Type in question title").ConfigureAwait(false);
            message = await WaitForMessage(ctx.Channel).ConfigureAwait(false);
            if (message == null) return;
            Question question = application.GetQuestion(message.Message.Content);
            if (question == null)
            {
                await ctx.Channel.SendMessageAsync("Question not found").ConfigureAwait(false);
                return;
            }

            await SetQuestionTitle(ctx, question).ConfigureAwait(false);

        }
        private static async Task SetQuestionTitle(CommandContext ctx, Question question)
        {
            await ctx.Channel.SendMessageAsync("Your next message will become this question's title.").ConfigureAwait(false);
            var message = await WaitForMessage(ctx.Channel).ConfigureAwait(false);
            if (message == null) return;
            question.ChangeTitle(message.Message.Content);
        }

        [Command("qd")]
        [RequireOwner]
        [Description("Changes a question's description.")]
        public async Task ChangeQuestionDescription(CommandContext ctx)
        {
            var question = await FindQuestion(ctx).ConfigureAwait(false);
            
            if (question == null)
            {
                await ctx.Channel.SendMessageAsync("Question not found").ConfigureAwait(false);
                return;
            }

            await SetQuestionDescription(ctx, question).ConfigureAwait(false);

        }
        private static async Task SetQuestionDescription(CommandContext ctx, Question question)
        {
            await ctx.Channel.SendMessageAsync("Your next message will become this question's description.").ConfigureAwait(false);
            var message = await WaitForMessage(ctx.Channel).ConfigureAwait(false);
            if (message == null) return;

            question.ChangeDescription(message.Message.Content);
        }

        [Command("qh")]
        [RequireOwner]
        [Description("Changes a question's help.")]
        public async Task ChangeQuestionHelp(CommandContext ctx)
        {
            var question = await FindQuestion(ctx);
            if (question == null)
            {
                await ctx.Channel.SendMessageAsync("Question not found").ConfigureAwait(false);
                return;
            }

            await SetQuestionHelp(ctx, question).ConfigureAwait(false);

        }
        private static async Task SetQuestionHelp(CommandContext ctx, Question question)
        {
            await ctx.Channel.SendMessageAsync("Your next message will become this question's help.").ConfigureAwait(false);
            
            var message = await WaitForMessage(ctx.Channel).ConfigureAwait(false);
            if (message == null) return;
            question.ChangeHelp(message.Message.Content);
        }

        [Command("cqt")]
        [RequireOwner]
        [Description("Changes a question's type if it's a short answer it becomes a long answer and vice versa.")]
        public async Task ChangeQuestionTypeCommand(CommandContext ctx)
        {
            var question = await FindQuestion(ctx).ConfigureAwait(false);
            if (question == null)
            {
                await ctx.Channel.SendMessageAsync("Question not found").ConfigureAwait(false);
                return;
            }

            question.ChangeQuestionType();
            await ctx.Channel.SendMessageAsync("Question Type Changed").ConfigureAwait(false);
        }

        [Command("saq")]
        [RequireOwner]
        [Description("Shows all the questions currently inside an application")]
        public async Task ShowApplicationQuestions(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync("Type in application title").ConfigureAwait(false);
            var message = await WaitForMessage(ctx.Channel).ConfigureAwait(false);
            if (message == null) return;
            Application application = GetApplication(message.Message.Content);
            if (application == null)
            {
                await ctx.Channel.SendMessageAsync("Application not found.").ConfigureAwait(false);
                return;
            }

            string text = application.Title + " Application Questions :\n" + "Application Roles :\n";
            foreach (ulong id in application.RoleIdList) text += id.ToString() + "\n";
            if (application.RoleIdList.Count == 0) text += "No roles given in this application.\n";
            text += "\n";

            foreach (Question q in application.Questions)
            {
                string questionStructure =  "Title :" + q.Title + "\n" +
                                            "Description :" + q.Description + "\n" +
                                            "Help :" + q.Help + "\n" +
                                            "Short Question :" + q.ShortQuestion + "\n\n";
                if (text.Length + questionStructure.Length > 2000)
                {
                    await ctx.Channel.SendMessageAsync(text);
                    text = "";
                    await Task.Delay(200);
                }
                text += questionStructure;              
            }
            await ctx.Channel.SendMessageAsync(text);
        }

        [Command("la")]
        [RequireOwner]
        [Description("Lists all currently existing applications")]
        public async Task ListApplications(CommandContext ctx)
        {
            if (Applications.Count == 0)
            {
                await ctx.Channel.SendMessageAsync("No applications exist");
                return;
            }

            List<string> applicationTitleCollections = new List<string>();
            string currentCollection = "";
            int i = 0;
            foreach (Application application in Applications)
            {
                if(currentCollection.Length + application.Title.Length >= 2000)
                {
                    applicationTitleCollections.Add(currentCollection);
                    currentCollection = "";
                }
                
                currentCollection += application.Title + "\n";
                i++;
                if (i >= Applications.Count) applicationTitleCollections.Add(currentCollection);
            }

            foreach(string titleCollection in applicationTitleCollections)
            {
                await ctx.Channel.SendMessageAsync(titleCollection);
                await Task.Delay(200);
            }
        }

        [Command("fs")]
        [RequireOwner]
        [Description("Saves all data manualy")]
        [Aliases("forcesave")]
        public async Task ForceSave(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync("Starting Save").ConfigureAwait(false);
            Save();
            await ctx.Channel.SendMessageAsync("Finished Save").ConfigureAwait(false);
        }



        /// <summary>
        /// 
        /// Debug, system IO stuff bellow
        /// 
        /// <returns>


        public static void Save()
        {
            SaveChannelIds();
            SaveRoleIds();
            SaveApplications();
            SavePendingApplications();
            SaveInteractableFinishedApplications();
            SaveReactionPosts();
        }

        private static void SaveRoleIds()
        {
            Dictionary<string, object> roleIds = new Dictionary<string, object>();
            roleIds.TryAdd(ROLE_IDS, recruitmentTeam);
            SavingSystem.Save(GUILD_DATA_FOLDER, roleIds);
        }
        private static void SaveChannelIds()
        {
            Dictionary<string, object> channelIds = new Dictionary<string, object>();
            channelIds.TryAdd(PENDING_CHANNEL_ID, pendingChannel);
            channelIds.TryAdd(FINISHED_CHANNEL_ID, finishedChannel);
            SavingSystem.Save(GUILD_DATA_FOLDER, channelIds);
        }
        private static void SaveApplications()
        {
            Dictionary<string, object> applicationStates = new Dictionary<string, object>();
            foreach (Application application in Applications) 
                applicationStates.TryAdd(application.Title + APPLICATION_EXTENTION, application.CaptureState());
            SavingSystem.Save(APPLICATION_FOLDER, applicationStates);
        }      
        private static void SavePendingApplications()
        {
            Dictionary<string, object> applicationStates = new Dictionary<string, object>();
            foreach (PendingApplication application in PendingApplications) 
                applicationStates.TryAdd(application.Title + PENDING_APPLICATION_EXTENTION, application.CaptureState());
            SavingSystem.Save(PENDING_FOLDER, applicationStates);
            PendingApplications.Clear();
        }
        private static void SaveInteractableFinishedApplications()
        {
            Dictionary<string, object> applicationStates = new Dictionary<string, object>();
            foreach (InteractableFinishedApplication application in InteractableFinishedApplications) 
                applicationStates.TryAdd(application.Message.Id.ToString()+ "_" + application.Message.ChannelId.ToString() + FINISHED_APPLICATION_EXTENTION, application.CaptureState());
            SavingSystem.Save(FINISHED_FOLDER, applicationStates);
            InteractableFinishedApplications.Clear();
        }
        private static void SaveReactionPosts()
        {
            Dictionary<string, object> postStates = new Dictionary<string, object>();
            foreach (ReactionPost reactionPost in ReactionPosts) 
                postStates.TryAdd(reactionPost.Application_.Title + reactionPost.DiscordMessage_.Id.ToString() + REACTION_POST_EXTENTION, reactionPost.CaptureState());
            SavingSystem.Save(REACTION_FOLDER, postStates);
            ReactionPosts.Clear();
        }
        private void SaveAutoSaveData()
        {
            Dictionary<string,object> saveData = new Dictionary<string, object>() { { AUTO_SAVE_DATA, AutoSaveData } };
            SavingSystem.Save(GUILD_DATA_FOLDER, saveData);
        }


        private static void Log(string v, ConsoleColor color = ConsoleColor.White)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(v, color);
            Console.ResetColor();
        }
    }
}
