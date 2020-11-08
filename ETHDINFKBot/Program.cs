using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DuckSharp;

namespace ETHDINFKBot
{
    class Program
    {
        private DiscordSocketClient Client;
        private CommandService commands;

        private IServiceProvider services;
        public static IConfiguration Configuration;
        public static string DiscordToken { get; set; }

        static void Main(string[] args)
        {
            Configuration = new ConfigurationBuilder()
              .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
              .Build();

            DiscordToken = Configuration["DiscordToken"];

            new Program().MainAsync(DiscordToken).GetAwaiter().GetResult();

        }

        public async Task MainAsync(string token)
        {



            Client = new DiscordSocketClient();

            Client.MessageReceived += HandleCommandAsync;



            await Client.LoginAsync(TokenType.Bot, token);
            await Client.StartAsync();

#if DEBUG
            await Client.SetGameAsync($"DEV MODE");
#else
            await Client.SetGameAsync($"DiskMath");
#endif

            services = new ServiceCollection()
                .AddSingleton(Client)
                .AddSingleton<InteractiveService>()
                .BuildServiceProvider();

            commands = new CommandService();
            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }


        public async Task HandleCommandAsync(SocketMessage m)
        {
            if (!(m is SocketUserMessage msg)) return;

            int argPos = 0;
            if (!(msg.HasStringPrefix(".", ref argPos)))
            {
                return;
            }

            var context = new SocketCommandContext(Client, msg);
            await commands.ExecuteAsync(context, argPos, services);
        }

        private async static void Test()
        {
            
        }
    }
}
