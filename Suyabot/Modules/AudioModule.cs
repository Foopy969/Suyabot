using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using Suyabot.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Suyabot.Services.AudioService;

namespace Suyabot.Modules
{
    public class AudioModule : ModuleBase<SocketCommandContext>
    {

        [Command("join", RunMode = RunMode.Async)]
        public async Task Join()
        {
            IVoiceChannel vc = (Context.User as IGuildUser)?.VoiceChannel;

            if (vc == null)
            {
                await Context.Channel.SendMessageAsync("**:x: You have to be in a voice channel to use this command**");
                return;
            }

            MessageChannel = Context.Channel;
            Client = await vc.ConnectAsync();
            await Context.Channel.SendMessageAsync($"**:thumbsup: Joined `{vc.Name}` and bound to {(MessageChannel as SocketTextChannel).Mention}**");
        }

        [Command("play", RunMode = RunMode.Async)]
        [Alias("p")]
        public async Task Play(string url)
        {
            if (MessageChannel == null) await Join();

            await Context.Channel.SendMessageAsync($"**:musical_note: Searching :mag_right: `{url}`**");

            if (!Enqueue(url, out string err))
            {
                await Context.Channel.SendEmbedAsync($"An error has occured", err);
                return;
            }

            Song song = GetSongs.Last();

            EmbedBuilder embed = new EmbedBuilder
            {
                Color = Color.Purple,
                Author = new EmbedAuthorBuilder
                {
                    IconUrl = Context.User.GetAvatarUrl(),
                    Name = "Added to queue"
                },
                ThumbnailUrl = song.Thumbnail,
                Title = song.Title,
                Url = song.Url
            };

            embed.AddField("Channel", song.Artist, true);
            embed.AddField("Song Duration", $"{song.Duration / 60}:{song.Duration % 60}", true);
            embed.AddField("Estimated Time Until Playing", "XX:XX", true);
            embed.AddField("Position in queue", GetSongs.Count());

            await Context.Channel.SendMessageAsync("", false, embed.Build());

            if (!IsStreaming) await PlayAsync(Client, MessageChannel);
        }

        [Command("skip")]
        public async Task Skip(int count = 0)
        {
            if (!AudioService.Skip(count, out string err))
            {
                await Context.Channel.SendEmbedAsync($"An error has occured", err);
                return;
            }

            await Context.Channel.SendMessageAsync($"***:fast_forward: Skipped :thumbsup:***");
        }

        [Command("remove")]
        [Alias("rm")]
        public async Task Remove(int index)
        {
            if (!Dequeue(index, out string err))
            {
                await Context.Channel.SendEmbedAsync($"An error has occured", err);
                return;
            }
            else
            {
                await Context.Channel.SendMessageAsync($"**:white_check_mark: Removed `{err}`**");
            }
        }

        [Command("queue")]
        [Alias("q")]
        public async Task Queue()
        {
            EmbedBuilder embed = new EmbedBuilder
            {
                Color = Color.Purple,
                Title = $"Queue for {Context.Guild.Name}",
                Url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ"
            };

            string text = "__Now Playing:__\n";

            if (!IsStreaming)
            {
                text = "Nothing is playing";
            }
            else
            {
                text += Current;

                if (GetSongs.Any())
                {
                    text += "$__Up Next:__\n"
                    + string.Join("\n", GetSongs.Select((x, i) => $"`{i + 1}.` {x}"))
                    + $"**{GetSongs.Count()} songs in queue | {GetSongs.Sum(x => x.Duration) / 60}:{GetSongs.Sum(x => x.Duration) % 60} total length**";
                }
            }

            embed.Description = text;

            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("disconnect")]
        [Alias("dc")]
        public async Task Disconnect()
        {
            MessageChannel = null;
            await Client.StopAsync();
            await Context.Channel.SendMessageAsync($"**:mailbox_with_no_mail: Successfully disconnected**");
        }

        [Command("np")]
        public async Task Np()
        {
            await Context.Channel.SendEmbedAsync($"Not a thing yet", "lol");
        }

        [Command("test")]
        public async Task Test()
        {
            Console.WriteLine(string.Join("\n", GetSongs.Select(x => x.Audio)));
        }
    }
}
