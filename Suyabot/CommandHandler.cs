using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Suyabot
{
    internal class CommandHandler
    {
        private DiscordSocketClient _client;
        private CommandService _service;

        private DateTime _today;
        private DateTime _timeout;
        private List<ulong> _claimed;

        public CommandHandler(DiscordSocketClient client)
        {
            _today = DateTime.Today;
            _timeout = DateTime.Now;
            _claimed = new List<ulong>();

            _client = client;
            _service = new CommandService();

            _service.AddModulesAsync(Assembly.GetEntryAssembly(), null);
            _service.Log += HandleLogAsync;
            _client.MessageReceived += HandleCommandAsync;
            _client.UserVoiceStateUpdated += HandleVoiceAsync;
            _client.MessageDeleted += HandleDeleteAsync;
        }

        private async Task HandleDeleteAsync(Cacheable<IMessage, ulong> arg1, ISocketMessageChannel arg2)
        {
            Extensions.Log("debug", "message deleted");
        }

        private async Task HandleLogAsync(LogMessage arg)
        {
            Extensions.Log(arg.Severity.ToString(), arg.Message);
        }

        private async Task HandleCommandAsync(SocketMessage s)
        {
            var msg = s as SocketUserMessage;
            if (msg == null) return;
            var context = new SocketCommandContext(_client, msg);
            if (context.User.IsBot) return;
            int argPos = 0;

            if (msg.HasStringPrefix(Config.Prefix, ref argPos))
            {
                Extensions.Log("Debug", $"Command received {msg.Content.Split('\n')[0]}");
                IResult result = await _service.ExecuteAsync(context, argPos, null);
                if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                {
                    Extensions.Log("Error", result.Error.Value.ToString());
                }
            }
            else
            {
                //a bad dadbot clone
                foreach (string prefix in new string[]{ "im ", "i'm ", "i am ", "iam "})
                {
                    if (msg.Content.ToLower().IndexOf(prefix) == 0)
                    {
                        await context.Channel.SendMessageAsync($"Hi {msg.Content.Remove(0, prefix.Length)}, I'm Dad.");
                        break;
                    }
                }

                if (msg.Content.ToLower() == "jo")
                {
                    await context.Channel.SendMessageAsync("Good morning, motherfuckers☆");
                }
            }
        }

        private async Task HandleVoiceAsync(SocketUser user, SocketVoiceState oldState, SocketVoiceState newState)
        {
            SocketGuild guild = oldState.VoiceChannel == null ? newState.VoiceChannel.Guild : oldState.VoiceChannel.Guild;
            ulong channelID = 0;

            if (oldState.VoiceChannel != newState.VoiceChannel)
            {
                if (DateTime.Today != _today)
                {
                    _today = DateTime.Today;
                    _claimed = new List<ulong>();
                    Extensions.Log("Info", "Daily claim has been reset");
                }

                int index = Config.Profiles.FindIndex(x => x.UserID == user.Id);
                if (index > -1 && !_claimed.Contains(user.Id))
                {
                    EmbedBuilder embed = new EmbedBuilder
                    {
                        Color = Color.Purple,
                        Author = new EmbedAuthorBuilder
                        {
                            IconUrl = user.GetAvatarUrl(),
                            Name = user.Username
                        }
                    };
                    _claimed.Add(user.Id);

                    if (Config.Profiles[index].Add(50, 100))
                    {
                        embed.WithDescription($"You leveled up to Lv.{Config.Profiles[index].Level}");
                    }
                    else
                    {
                        embed.WithDescription("Daily claimed");
                    }

                    Config.Write(Config.ProfilesPath, Config.Profiles);
                    await guild.DefaultChannel.SendMessageAsync("", false, embed.Build());
                }

                if (Config.GetGuildChannel(guild.Id, ref channelID))
                {
                    if (DateTime.Now.Subtract(_timeout).TotalSeconds < 1)
                    {
                        Extensions.Log("Info", "Log timeout");
                        return;
                    }
                    else
                    {
                        _timeout = DateTime.Now;
                    }

                    await guild.GetTextChannel(channelID).SendMessageAsync(null, false, GetVoiceLogEmbed(user, oldState.VoiceChannel, newState.VoiceChannel));
                }
            }
        }

        private Embed GetVoiceLogEmbed(SocketUser user, SocketVoiceChannel oldChannel, SocketVoiceChannel newChannel)
        {
            EmbedBuilder embed = new EmbedBuilder 
            { 
                Color = Color.Purple,
            };
            if (newChannel == null)
                embed.WithDescription($"{user.GetText()} has left `{oldChannel}`");
            else if (oldChannel == null)
                embed.WithDescription($"{user.GetText()} has joined `{newChannel}`");
            else
                embed.WithDescription($"{user.GetText()} has moved from `{oldChannel}` to `{newChannel}`");
            return embed.Build();
        }
    }
}