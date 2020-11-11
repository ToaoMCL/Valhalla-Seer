using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using System.Threading.Tasks;
using System.Collections.Generic;
using Valhalla_Seer.DataStructures;
using Valhalla_Seer.Saving;
using System;

namespace Valhalla_Seer.Commands
{
    public class ApplicationCommands
    {

        // TEST CHANNEL NAMES
        const ulong PENDING_RESPONSES_CHANNEL = 774273745418321930;
        const ulong FINISHED_APPLICATION_CHANEL = 775447951405350992;
        const ulong TEST_ROLE = 771403730825904128;
        // END OF TEST

        const string
            APPLICATIONS = "Applications.aplc",
            APPLICATIONS_IN_PROGRESS = "ApplicationsInProgress.aplc",
            FINISHED_APPLICATIONS = "PendingApplications.aplc";


        static List<Application> applications = new List<Application>();

        static List<PendingApplication> pendingApplications = new List<PendingApplication>();

        static bool InitRan = false; // Just to make sure it's not ran a second time but I should look for a better way of handling this

        // GUID > Application
        Dictionary<string, ApplicationInProgress> applicationsInProgress = new Dictionary<string, ApplicationInProgress>();

        // Finished Application > MessageID ?
        


        public static async Task Init()
        {
            if (InitRan) return;
            await LoadApplications().ConfigureAwait(false);
            InitRan = true;
        }

        static async Task LoadApplications()
        {
            Console.WriteLine("Loading Application Forms");
            Dictionary<string, object> loadedApplications = SavingSystem.Load(APPLICATIONS);
            foreach (object application in loadedApplications.Values)
            {
                Application loadedApplication = new Application("_LoadedApplication_");
                await loadedApplication.RestoreState(application).ConfigureAwait(false);
                applications.Add(loadedApplication);
                Log("Application " + loadedApplication.Title + " : Loaded", ConsoleColor.Green);
            }


            Console.WriteLine("Loading Finished Applications");
            Dictionary<string, object> loadedFinishedApplications = SavingSystem.Load(FINISHED_APPLICATIONS);
            foreach (object loadedFinishedApplication in loadedFinishedApplications.Values)
            {
                try
                {
                    PendingApplication application = new PendingApplication(null, null, null, loadedFinishedApplication);
                    await application.RestoreState(loadedFinishedApplication).ConfigureAwait(false);
                    Log(
                        "Pending Application " + application.Message.Id +
                        " for " + application.Message.Channel.GuildId +
                        " in " + application.Message.ChannelId + " : Loaded", ConsoleColor.Green);
                    pendingApplications.Add(application);

                    AddInteractivity(application);
                }
                catch
                {
                    Log("Dead application found and skipped", ConsoleColor.Red);
                }
            }
        }

        private static void Log(string v, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(v, color);
            Console.ResetColor();
        }

        private static async void AddInteractivity(PendingApplication application)
        {
            var applicationEmbed = new DiscordEmbedBuilder
            {
                Title = application.Title,
                Description = application.ApplicantRespones,
                ThumbnailUrl = application.Applicant.AvatarUrl,
                Color = DiscordColor.Yellow
            };

            var thumbsUpEmoji = DiscordEmoji.FromName(Bot.Client, ":+1:");
            var thumbsDownEmoji = DiscordEmoji.FromName(Bot.Client, ":-1:");

            var interactivity = Bot.Client.GetInteractivityModule();
            var reactionResult = await interactivity.WaitForMessageReactionAsync(
            x => x.Name == thumbsUpEmoji ||
            x.Name == thumbsDownEmoji,
            application.Message).ConfigureAwait(false);

            var role = application.Message.Channel.Guild.GetRole(TEST_ROLE);
            if (reactionResult.Emoji == thumbsUpEmoji)
            {
                await application.Applicant.GrantRoleAsync(role).ConfigureAwait(false);
                await application.Applicant.SendMessageAsync("Your application for " + applicationEmbed.Title + " has been aprooved").ConfigureAwait(false);
                applicationEmbed.Color = DiscordColor.Green;
            }
            else if (reactionResult.Emoji == thumbsDownEmoji)
            {
                await application.Applicant.SendMessageAsync("Your application for " + applicationEmbed.Title + " has been denied").ConfigureAwait(false);
                applicationEmbed.Color = DiscordColor.Red;
            }

            applicationEmbed.Description = reactionResult.User.Username + " has aprooved this application:" + Environment.NewLine + applicationEmbed.Description;
            await application.Message.Channel.Guild.GetChannel(id: FINISHED_APPLICATION_CHANEL).SendMessageAsync(embed: applicationEmbed).ConfigureAwait(false);
            await Task.Delay(200);
            var chanel = application.Message.Channel.Guild.GetChannel(PENDING_RESPONSES_CHANNEL);
            await chanel.DeleteMessageAsync(application.Message).ConfigureAwait(false);
            pendingApplications.Remove(application);
        }

        /// <summary>
        /// ###########
        /// 
        /// End Of Init
        /// 
        /// ###########
        /// </returns>

        [Command("sa")]
        public async Task StartApplication(CommandContext ctx, string applicationAppliedFor)
        {
            Application application = null;
            foreach (Application app in applications)
            {
                if (app.Title == applicationAppliedFor)
                {
                    application = app;
                    break;
                }
            }

            if (application == null) return;


            int questionsAnswered = 0;
            ApplicationInProgress applicationInProgress = new ApplicationInProgress(ctx, application);
            foreach (Question question in application.Questions)
            {
                await SendQuestion(ctx, question, applicationInProgress).ConfigureAwait(false);
                questionsAnswered++;
            }

            if (questionsAnswered < application.Questions.Count) return;

            await ctx.Member.SendMessageAsync("Application Finished").ConfigureAwait(false);

            var applicationEmbed = new DiscordEmbedBuilder
            {
                Title = applicationInProgress.Title,
                Description = applicationInProgress.GetApplicationResults(),
                ThumbnailUrl = ctx.Client.CurrentUser.AvatarUrl,
                Color = DiscordColor.Yellow
            };
            var applicationMessage = await ctx.Guild.GetChannel(id: PENDING_RESPONSES_CHANNEL).SendMessageAsync(embed: applicationEmbed).ConfigureAwait(false);

            // real iffy here
            PendingApplication pendingApplication = new PendingApplication(applicationMessage, ctx.Member, applicationInProgress, applicationEmbed.Description);
            pendingApplications.Add(pendingApplication);

            SaveFinishedApplications();

            // reaction role assignment
            var thumbsUpEmoji = DiscordEmoji.FromName(ctx.Client, ":+1:");
            var thumbsDownEmoji = DiscordEmoji.FromName(ctx.Client, ":-1:");
            DiscordEmoji[] emojis = new DiscordEmoji[2] { thumbsUpEmoji, thumbsDownEmoji };

            await AddReactions(applicationMessage, emojis).ConfigureAwait(false);

            // I don't actualy know
            AddInteractivity(pendingApplication);


            //// actual reaction interactivity for application confirmation
            //var interactivity = ctx.Client.GetInteractivityModule();
            //var reactionResult = await interactivity.WaitForMessageReactionAsync(
            //x => x.Name == thumbsUpEmoji ||
            //x.Name == thumbsDownEmoji,
            //applicationMessage).ConfigureAwait(false);

            //var role = ctx.Guild.GetRole(TEST_ROLE);
            //if (reactionResult.Emoji == thumbsUpEmoji)
            //{
            //    await ctx.Member.GrantRoleAsync(role).ConfigureAwait(false);
            //    await ctx.Member.SendMessageAsync("Your application for " + application.Title + " has been aprooved").ConfigureAwait(false);
            //    applicationEmbed.Color = DiscordColor.Green;
            //}
            //else if (reactionResult.Emoji == thumbsDownEmoji)
            //{
            //    await ctx.Member.SendMessageAsync("Your application for " + application.Title + " has been denied").ConfigureAwait(false);
            //    applicationEmbed.Color = DiscordColor.Red;
            //}
            
            //applicationEmbed.Description = reactionResult.User.Username + " has aprooved this application:" + Environment.NewLine + applicationEmbed.Description;
            //await ctx.Guild.GetChannel(id: FINISHED_APPLICATION_CHANEL).SendMessageAsync(embed: applicationEmbed).ConfigureAwait(false);
            //await Task.Delay(200);
            //var chanel = ctx.Guild.GetChannel(PENDING_RESPONSES_CHANNEL);
            //await chanel.DeleteMessageAsync(applicationMessage).ConfigureAwait(false);
        }

        private static async Task AddReactions(DiscordMessage applicationMessage, DiscordEmoji[] emojis)
        {
            foreach(DiscordEmoji emoji in emojis)
            {
                await applicationMessage.CreateReactionAsync(emoji).ConfigureAwait(false);
                await Task.Delay(200);
            }
        }

        private async Task SendQuestion(CommandContext ctx, Question question, ApplicationInProgress newApplication)
        {
            var thumbsUpEmoji = DiscordEmoji.FromName(ctx.Client, ":+1:");
            var thumbsDownEmoji = DiscordEmoji.FromName(ctx.Client, ":-1:");
            var questionMarkEmoji = DiscordEmoji.FromName(ctx.Client, ":question:");
            DiscordEmoji[] emojis = new DiscordEmoji[3] { thumbsUpEmoji, thumbsDownEmoji, questionMarkEmoji };

            var applicationEmbed = new DiscordEmbedBuilder
            {
                Title = question.Title,
                Description = question.Description,
                ThumbnailUrl = ctx.Client.CurrentUser.AvatarUrl,
                Color = DiscordColor.Green
            };

            var message = await ctx.Member.SendMessageAsync(embed: applicationEmbed).ConfigureAwait(false);
            await AddReactions(message, emojis).ConfigureAwait(false);

            var interactivity = ctx.Client.GetInteractivityModule();

            while(true)            
            {
                var reactionResult = await interactivity.WaitForMessageReactionAsync(
                x => x.Name == thumbsUpEmoji ||
                x.Name == thumbsDownEmoji ||
                x.Name == questionMarkEmoji,
                message).ConfigureAwait(false);

                var role = ctx.Guild.GetRole(TEST_ROLE);
                if (reactionResult.Emoji == thumbsUpEmoji) newApplication.AddResponse(question.Title + Environment.NewLine + "Yes" + Environment.NewLine);
                else if (reactionResult.Emoji == thumbsDownEmoji) newApplication.AddResponse(question.Title + Environment.NewLine + "No" + Environment.NewLine);

                if (reactionResult.Emoji != questionMarkEmoji) break;
                await message.Channel.SendMessageAsync("help requested").ConfigureAwait(false);
            }
        }

        [Command("ca")]
        public async Task CreateApplication(CommandContext ctx, string applicationTitle)
        {
            foreach(Application application in applications)
            {
                if (application.Title != applicationTitle) continue;
                await ctx.Channel.SendMessageAsync("Application Already Exists");
                return;
            }

            applications.Add(new Application(applicationTitle));
            SaveApplications();
        }

        [Command("la")]
        public async Task ListApplications(CommandContext ctx)
        {
            if(applications.Count == 0)
            {
                await ctx.Channel.SendMessageAsync("No applications exist");
            }

            foreach(Application application in applications)
            {
                await ctx.Channel.SendMessageAsync(application.Title);
                await Task.Delay(200);
            }
        }

        [Command("cq")]
        public async Task CreateQuestion(CommandContext ctx, string applicationTitle, string questionTitle, string questionDescription)
        {
            foreach(Application application in applications)
            {
                if(application.Title == applicationTitle)
                {
                    application.AddQuestion(questionTitle, questionDescription);
                    SaveApplications();
                    return;
                }
            }
        }

        [Command("saq")]
        public async Task ShowApplication(CommandContext ctx, string applicationTitle)
        {
            foreach(Application application in applications)
            {
                if (application.Title != applicationTitle) continue;

                if (application.Questions.Count == 0) await ctx.Channel.SendMessageAsync(application.Title + " has no questions");

                foreach (Question q in application.Questions)
                {
                    await ctx.Channel.SendMessageAsync(q.Title + ": " + q.Description);
                    await Task.Delay(200);
                }

                return;

            }
        }



        public void SaveApplications()
        {
            List<object> applicationStates = new List<object>();
            foreach(Application application in applications) applicationStates.Add(application.CaptureState());
            SavingSystem.Save(APPLICATIONS, applicationStates);
        }
        
        public void SaveFinishedApplications()
        {
            List<object> finishedApplicationStates = new List<object>();
            foreach (PendingApplication application in pendingApplications) finishedApplicationStates.Add(application.CaptureState());
            SavingSystem.Save(FINISHED_APPLICATIONS, finishedApplicationStates);
        }
    }
}
