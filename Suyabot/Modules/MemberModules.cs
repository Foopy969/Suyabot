using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Suyabot.Modules
{
    [Group("member")]
    public class MemberModules : ModuleBase<SocketCommandContext>
    {
        [Command("join")]
        public async Task Join()
        {
            if (Config.Profiles.Any(x => x.UserID == Context.User.Id))
            {
                await Context.Channel.SendEmbedAsync($"{Context.User.Username} you've already joined");
                return;
            }
            Extensions.Log("Info", $"Created profile for {Context.User.Username}");
            Config.Profiles.Add(new Profile(Context.User.Id));
            Config.Write(Config.ProfilesPath, Config.Profiles);
            await Context.Channel.SendEmbedAsync($"Thanks for joining", "There is literally nothing you can do now.");
        }

        [Command("profile")]
        public async Task Profile(SocketUser user = null)
        {
            user = user == null ? Context.User : user;
            int index = Config.Profiles.FindIndex(x => x.UserID == user.Id);
            if (index == -1)
            {
                Extensions.Log("Error", $"Couldn't find profile for {user.Username}");
                await Context.Channel.SendEmbedAsync($"Error", $"Couldn't find profile for {user.GetText()}");
                return;
            }

            Profile profile = Config.Profiles[index];
            EmbedBuilder embed = new EmbedBuilder
            {
                Color = Color.Purple,
                Author = new EmbedAuthorBuilder
                {
                    IconUrl = user.GetAvatarUrl(),
                    Name = user.Username
                }
            };
            embed.AddField("Level", profile.Level);
            embed.AddField("Credit", profile.Credit);
            embed.AddField("Exp", $"{profile.GetExpBar()} ({profile.Exp} / {profile.GetMaxExp()})");
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("give")]
        public async Task Give(SocketUser target, int amount)
        {
            int userIndex = Config.Profiles.FindIndex(x => x.UserID == Context.User.Id);
            int targetIndex = Config.Profiles.FindIndex(x => x.UserID == target.Id);

            if (userIndex == -1)
            {
                Extensions.Log("Error", $"Couldn't find profile for {Context.User.Username}");
                await Context.Channel.SendEmbedAsync($"Error", $"Couldn't find profile for {Context.User.GetText()}");
                return;
            }
            else if (targetIndex == -1)
            {
                Extensions.Log("Error", $"Couldn't find profile for {target.Username}");
                await Context.Channel.SendEmbedAsync($"Error", $"Couldn't find profile for {target.GetText()}");
                return;
            }
            else if (userIndex == targetIndex)
            {
                Extensions.Log("Error", $"Duplicate profile");
                await Context.Channel.SendEmbedAsync($"{Context.User.GetText()} gave him/herself money lmao");
                return;
            }
            else if (amount <= 0)
            {
                Extensions.Log("Error", $"Invalid amount");
                await Context.Channel.SendEmbedAsync($"Error", $"`{amount}` is not a valid amount");
                return;
            }
            else if (Config.Profiles[userIndex].Credit < amount)
            {
                Extensions.Log("Error", $"Invalid amount");
                await Context.Channel.SendEmbedAsync($"`{Context.User.GetText()}` you don't have anough credit");
                return;
            }

            Config.Profiles[userIndex].Credit -= amount;
            Config.Profiles[targetIndex].Credit += amount;
            Config.Write(Config.ProfilesPath, Config.Profiles);

            await Context.Channel.SendEmbedAsync($"{Context.User.GetText()} gave `{amount}` credit to {target.GetText()}");
            return;
        }

        [Command("betroll")]
        public async Task Betroll(int wager)
        {
            int index = Config.Profiles.FindIndex(x => x.UserID == Context.User.Id);
            if (index == -1)
            {
                Extensions.Log("Error", $"Couldn't find profile for {Context.User.Username}");
                await Context.Channel.SendEmbedAsync($"Error", $"Couldn't find profile for {Context.User.GetText()}");
                return;
            }
            else if (wager <= 0)
            {
                Extensions.Log("Error", $"Invalid amount");
                await Context.Channel.SendEmbedAsync($"Error", $"`{wager}` is not a valid wager");
                return;
            }
            else if (Config.Profiles[index].Credit < wager)
            {
                Extensions.Log("Error", $"Invalid amount");
                await Context.Channel.SendEmbedAsync($"`{Context.User.GetText()}` you don't have anough credit");
                return;
            }

            int num = new Random().Next(100) + 1;
            int multiplier = GetMultiplier(num);

            Config.Profiles[index].Credit += wager * (multiplier - 1);
            Config.Write(Config.ProfilesPath, Config.Profiles);

            await Context.Channel.SendEmbedAsync($"{Context.User.Username} you rolled the number `{num}` and got a `{multiplier}x` multiplier.");
            return;
        }

        [Command("rank")]
        public async Task Rank()
        {
            List<Profile> profiles = new List<Profile>();
            foreach (var profile in Config.Profiles)
            {
                if (Context.Guild.GetUser(profile.UserID) != null)
                {
                    profiles.Add(profile);
                }
            }
            profiles = profiles.OrderBy(x => x.GetMaxExp() + x.Exp).ToList();
            string text = "`member ranking`\n```css";
            for (int i = 0; i < profiles.Count(); i++)
            {
                text += $"\n{1 + i}. {Context.Guild.GetUser(profiles[i].UserID).Username}";
            }
            await Context.Channel.SendMessageAsync(text + "\n```");
        }

        private int GetMultiplier(int num)
        {
            if (num == 100)
                return 10;
            else if (num > 89)
                return 4;
            else if (num > 65)
                return 2;
            else
                return 0;
        }
    }
}
