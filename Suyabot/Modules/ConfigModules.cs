using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace Suyabot.Modules
{
    [Group("config")]
    [Alias("cfg", "set")]
    public class ConfigModules : ModuleBase<SocketCommandContext>
    {
        [Command("prefix")]
        [RequireOwner]
        public async Task Prefix(string prefix)
        {
            Extensions.Log("Info", $"Prefix is set to {prefix}");
            Config.Reader.Prefix = prefix;
            Config.Write(Config.ConfigPath, Config.Reader);
            await Context.Channel.SendEmbedAsync($"Prefix is set to `{prefix}`");
        }

        [Command("mention")]
        public async Task Mention()
        {
            Config.Reader.Mention = !Config.Mention;
            Config.Write(Config.ConfigPath, Config.Reader);
            if (Config.Mention)
                await Context.Channel.SendEmbedAsync("Started mentioning users");
            else
                await Context.Channel.SendEmbedAsync("Stopped mentioning users");
        }

        [Command("status")]
        public async Task Status(params string[] status)
        {
            await Context.Client.SetGameAsync(string.Join(" ", status));
            await Context.Channel.SendEmbedAsync("Status set");
        }

        [Group("log")]
        public class Log : ModuleBase<SocketCommandContext>
        {
            [Command("channel")]
            public async Task Chennel()
            {
                ulong channelID = 0;
                if (Config.GetGuildChannel(Context.Guild.Id, ref channelID))
                {
                    await Context.Channel.SendEmbedAsync($"Logging channel of this server is {Context.Guild.GetTextChannel(channelID).Mention}");
                }
                else
                {
                    await Context.Channel.SendEmbedAsync($"Logging channel wasn't set on this server");
                }

            }


            [Command("here")]
            public async Task Here()
            {
                if (Config.GuildExists(Context.Guild.Id))
                {
                    Extensions.Log("Info", $"Logging channel updated for {Context.Guild.Name}");
                    Config.Guilds[Config.Guilds.FindIndex(x => x.ServerID == Context.Guild.Id)].ChannelID = Context.Channel.Id;
                }
                else
                {
                    Extensions.Log("Info", $"Adding guild {Context.Guild.Name}");
                    Config.Guilds.Add(new Guild(Context.Guild.Id, Context.Channel.Id, false));
                    Config.Write(Config.GuildsPath, Config.Guilds);
                }

                await Context.Channel.SendEmbedAsync($"Logging channel set to {(Context.Channel as SocketTextChannel).Mention}");
            }

            [Command("start")]
            public async Task Start()
            {
                if (Config.GuildExists(Context.Guild.Id))
                {
                    Extensions.Log("Info", $"Logging started for {Context.Guild.Name}");
                    Config.Guilds[Config.Guilds.FindIndex(x => x.ServerID == Context.Guild.Id)].State = true;
                    ulong channelID = Config.Guilds[Config.Guilds.FindIndex(x => x.ServerID == Context.Guild.Id)].ChannelID;
                    await Context.Channel.SendEmbedAsync($"Started logging at {Context.Guild.GetTextChannel(channelID).Mention}");
                }
                else
                {
                    await Context.Channel.SendEmbedAsync("Error", "Unspecified logging channel");
                }
            }

            [Command("stop")]
            public async Task Stop()
            {
                if (Config.GuildExists(Context.Guild.Id))
                {
                    Extensions.Log("Info", $"Logging sopped for {Context.Guild.Name}");
                    Config.Guilds[Config.Guilds.FindIndex(x => x.ServerID == Context.Guild.Id)].State = false;
                    await Context.Channel.SendEmbedAsync($"Stopped logging");
                }
                else
                {
                    await Context.Channel.SendEmbedAsync("Error", "Unspecified logging channel");
                }
            }
        }
    }
}
