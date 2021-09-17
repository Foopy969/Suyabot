using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Suyabot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Suyabot.Services.AudioService;

namespace Suyabot.Modules
{
    public class AudioModule : ModuleBase<SocketCommandContext>
    {

        [Command("join", RunMode = RunMode.Async)]
        [Alias("bind")]
        public async Task Join()
        {
            IVoiceChannel vc = (Context.User as IGuildUser)?.VoiceChannel;

            if (vc == null)
            {
                await Context.Channel.SendMessageAsync("**:x: You have to be in a voice channel to use this command**");
                return;
            }

            try
            {
                Initialize(await vc.ConnectAsync(), Context.Channel);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                await Context.Channel.SendMessageAsync($"**:thumbsup: Joined `{vc.Name}` and bound to {(Context.Channel as SocketTextChannel).Mention}**");
            }
        }

        [Command("disconnect")]
        [Alias("dc")]
        public async Task Disconnect()
        {
            if (!IsBound)
            {
                await Context.Channel.SendMessageAsync("**:x: Not in a voice channel**");
            }
            else
            {
                AudioService.Dispose();
                await Context.Channel.SendMessageAsync($"**:mailbox_with_no_mail: Successfully disconnected**");
            }
        }

        [Command("play", RunMode = RunMode.Async)]
        [Alias("p")]
        public async Task Play(string url)
        {
            if (!IsBound) await Join();

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
            embed.AddField("Estimated Time Until Playing", $"TBI", true);
            embed.AddField("Position in queue", IsPlaying ? (GetSongs.Count() - 1).ToString() : "Now");

            await Context.Channel.SendMessageAsync("", false, embed.Build());

            if (!IsPlaying) await PlayAsync();
        }

        [Command("skip")]
        public async Task Skip(int count = 1)
        {
            if (!IsPlaying)
            {
                await Context.Channel.SendMessageAsync("**:x: Nothing is playing on this server**");
                return;
            }
            else if (!AudioService.Skip(count, out string err))
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
            if (!IsPlaying)
            {
                await Context.Channel.SendMessageAsync("**:x: Nothing is playing on this server**");
            }
            else
            {
                var songs = GetSongs;

                List<string> text = new List<string>();

                text.Add("__Now Playing:__");
                text.Add(songs[0].ToString());
                songs.RemoveAt(0);

                if (songs.Any())
                {
                    text.Add("__Up Next:__");
                    text.AddRange(songs.Select((x, i) => $"`{i + 1}.` {x}"));
                    text.Add($"**{songs.Count()} songs in queue | {songs.Sum(x => x.Duration) / 60}:{songs.Sum(x => x.Duration) % 60} total length**");
                }

                EmbedBuilder embed = new EmbedBuilder
                {
                    Color = Color.Purple,
                    Title = $"Queue for {Context.Guild.Name}",
                    Url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                    Description = string.Join("\n", text)
                };

                await Context.Channel.SendMessageAsync("", false, embed.Build());
            }

        }

        [Command("np")]
        public async Task Np()
        {
            await Context.Channel.SendEmbedAsync($"TBI", $"Help the development on [here](https://github.com/Foopy969/Suyabot/blob/master/Suyabot/Modules/AudioModule.cs)");
        }
    }
}
