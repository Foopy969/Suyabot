using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace Suyabot
{
    public static class Extensions
    {
        public static async Task<RestUserMessage> SendEmbedAsync(this ISocketMessageChannel channel, string text)
        {
            EmbedBuilder Embed = new EmbedBuilder 
            { 
                Color = Color.Purple, 
                Description = text
            };
            return await channel.SendMessageAsync(null, false, Embed.Build());
        }

        public static async Task<RestUserMessage> SendEmbedAsync(this ISocketMessageChannel channel, string title, string text)
        {
            EmbedBuilder Embed = new EmbedBuilder
            {
                Color = Color.Purple,
                Title = title,
                Description = text
            };
            return await channel.SendMessageAsync(null, false, Embed.Build());
        }

        public static string GetText(this SocketUser user)
        {
            if (Config.Mention)
                return user.Mention;
            else
                return user.Username;
        }

        public static void Log(string type, string text)
        {
            Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}][{type}]: {text}");
        }
    }
}
