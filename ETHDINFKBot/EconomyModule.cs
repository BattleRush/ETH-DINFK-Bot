using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot
{
    public class EconomyModule : ModuleBase<SocketCommandContext>
    {
        [Command("eco")]
        public async Task NekoGif()
        {
            var author = Context.Message.Author;



            Context.Channel.SendMessageAsync("", false);
        }

        [Command("balance")]
        public async Task GetBalance(string input)
        {
            input = input.ToLower();
            if (input == "init")
            {

            }

            var author = Context.Message.Author;



            Context.Channel.SendMessageAsync("", false);
        }

        [Command("loan")]
        public async Task GetLoan(string input)
        {
            input = input.ToLower();
            if (input == "init")
            {

            }

            var author = Context.Message.Author;



            Context.Channel.SendMessageAsync("", false);
        }

    }
}
