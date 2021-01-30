using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace Suyabot.Modules
{
    public class TestModules : ModuleBase<SocketCommandContext>
    {
        [Command("test")]
        public async Task Task(SocketUser user)
        {
            await Context.Channel.SendEmbedAsync(user.GetText());
        }
    }
}
