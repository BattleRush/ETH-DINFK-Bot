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
using ETHDINFKBot.Stats;
using Newtonsoft.Json;

namespace ETHDINFKBot
{
    class Program
    {
        private DiscordSocketClient Client;
        private CommandService commands;

        private IServiceProvider services;
        public static IConfiguration Configuration;
        public static string DiscordToken { get; set; }

        public static GlobalStats GlobalStats = new GlobalStats()
        {
            DiscordUsers = new List<DiscordUser>()
        };

        static void Main(string[] args)
        {
            Configuration = new ConfigurationBuilder()
              .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
              .Build();

            DiscordToken = Configuration["DiscordToken"];

            new Program().MainAsync(DiscordToken).GetAwaiter().GetResult();

        }
        public static void LoadStats()
        {
            if (File.Exists("Stats\\stats.json"))
            {
                string content = File.ReadAllText("Stats\\stats.json");
                GlobalStats = JsonConvert.DeserializeObject<GlobalStats>(content);
            }
        }
        public static void SaveStats()
        {
            string content = JsonConvert.SerializeObject(GlobalStats);
            File.WriteAllText("Stats\\stats.json", content);
        }

        public async Task MainAsync(string token)
        {
            if (!Directory.Exists("Logs"))
                Directory.CreateDirectory("Logs");
            if (!Directory.Exists("Stats"))
                Directory.CreateDirectory("Stats");

            LoadStats();

            Client = new DiscordSocketClient();

            Client.MessageReceived += HandleCommandAsync;



            await Client.LoginAsync(TokenType.Bot, token);
            await Client.StartAsync();

#if DEBUG
            await Client.SetGameAsync($"DEV MODE");
#else
            await Client.SetGameAsync($"with a neko");
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

        private static Dictionary<ulong, DateTime> SpamCache = new Dictionary<ulong, DateTime>();
        public async Task HandleCommandAsync(SocketMessage m)
        {
            if (!(m is SocketUserMessage msg)) return;



            var message = msg.Content;
            var randVal = msg.Author.DiscriminatorValue % 10;

            // TODO Different color for defcom bot

            switch (randVal)
            {
                case 0:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
                case 1:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case 2:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case 3:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    break;
                case 4:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case 5:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case 6:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case 7:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    break;
                case 8:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
                case 9:
                    Console.ForegroundColor = ConsoleColor.DarkMagenta;
                    break;
                default:
                    break;
            }



            Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {msg.Author} wrote: {msg.Content}");
            File.AppendAllText($"Logs\\ETHDINFK_{DateTime.Now:yyyy_MM_dd}_spam.txt", $"[{DateTime.Now:yyyy.MM.dd HH:mm:ss}] " + msg.Author + " wrote: " + msg.Content + Environment.NewLine);

            int argPos = 0;
            if (!(msg.HasStringPrefix(".", ref argPos)))
            {
                return;
            }

            if (m.Author.IsBot)
                return;


            if (SpamCache.ContainsKey(m.Author.Id))
            {
                if (SpamCache[m.Author.Id] > DateTime.Now.AddMilliseconds(-500))
                {
                    await m.Channel.SendMessageAsync($"Stop spamming <@{m.Author.Id}>");
                    await m.Channel.SendMessageAsync($"Stop spamming <@{m.Author.Id}>");
                    return;
                }

                SpamCache[m.Author.Id] = DateTime.Now;
            }
            else
            {
                SpamCache.Add(m.Author.Id, DateTime.Now);
            }

            Console.ResetColor();


            var context = new SocketCommandContext(Client, msg);
            await commands.ExecuteAsync(context, argPos, services);
        }

        private async static void Test()
        {

        }
    }
}
