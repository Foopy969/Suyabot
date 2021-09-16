using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace Suyabot.Modules
{
    public class TestModules : ModuleBase<SocketCommandContext>
    {
        [Command("help")]
        public async Task Help()
        {
            await Context.Channel.SendMessageAsync("```\n$play {url} #not complete\n$skip [count]\n$remove {index}\n$queue #not working\n$np #not working\n$disconnect```");
        }
    }
}
