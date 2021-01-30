using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace Suyabot
{
    class Program
    {
        private DiscordSocketClient _client;
        private CommandHandler _handler;

        static void Main(string[] args)
        => new Program().StartAsync().GetAwaiter().GetResult();

        public async Task StartAsync()
        {
            Extensions.Log("Info", $"Starting...");
            if (!Config.Read())
            {
                Console.WriteLine("press any key to continue...");
                Console.ReadKey();
                Environment.Exit(1);
            }
            _client = new DiscordSocketClient();
            await _client.LoginAsync(TokenType.Bot, Config.Token);
            await _client.StartAsync();
            _handler = new CommandHandler(_client);
            Extensions.Log("Info", $"Started");
            await Task.Delay(-1);
        }
    }
}
