using DSharpPlus;
using DSharpPlus.Interactivity;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Valhalla_Seer.Commands;

namespace Valhalla_Seer
{
    public class Bot
    {
        public static DiscordClient Client { get; private set; }

        public InteractivityModule Interactivity { get; private set; }
        public CommandsNextModule Commands { get; private set; }
        public async Task RunAsync()
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

            var config = new DiscordConfiguration
            {
                Token = configJson.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                LogLevel = LogLevel.Debug,
                UseInternalLogHandler = true
            };

            Client = new DiscordClient(config);

            Client.Ready += OnClientReady;

            Client.UseInteractivity(new InteractivityConfiguration
            {
                Timeout = TimeSpan.FromHours(36)
            }) ;

            var commandsConfig = new CommandsNextConfiguration
            {
                StringPrefix = configJson.Prefix,
                EnableDms = true,
                EnableMentionPrefix = true,
                CaseSensitive = false,
                IgnoreExtraArguments = true
            };

            Commands = Client.UseCommandsNext(commandsConfig);
            Commands.RegisterCommands<ApplicationCommands>();
            await ApplicationCommands.Init().ConfigureAwait(false);
            await Client.ConnectAsync();
            await Task.Delay(-1);
        }

        private Task OnClientReady(ReadyEventArgs e)
        {
            return Task.CompletedTask;
        }
    }
}
