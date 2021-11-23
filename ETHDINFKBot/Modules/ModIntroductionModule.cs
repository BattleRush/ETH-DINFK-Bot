using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ETHDINFKBot.Modules
{
    /*
    public class ModIntroduction : InteractiveBase
    {
        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
        [Command("ACP")]
        public async Task ACPanel()
        {
            var m = Context.Message;
            var author = Context.Message.Author;
            if (author.Id != 321022340412735509)
            {
                await Context.Channel.SendMessageAsync($"Unauthorized access atempt. Banning <@{author.Id}>", false);
                return;
            }

            await m.Channel.SendMessageAsync("https://i.gifer.com/embedded/download/8XAj.gif");
            await m.Channel.SendMessageAsync("**Entering Admin Control Panel**");
            Thread.Sleep(4000);
            await m.Channel.SendMessageAsync("**ACP open** Hello <@321022340412735509>");
            Thread.Sleep(3000);

            var selectOption = await m.Channel.SendMessageAsync(@"Loading options please wait");
            Thread.Sleep(2000);

            await selectOption.ModifyAsync(msg => msg.Content = @"Loading options please wait.
1) Give Marc Admin");
            Thread.Sleep(2000);

            await selectOption.ModifyAsync(msg => msg.Content = @"Loading options please wait..
1) Give Marc Admin
2) Delete Server");
            Thread.Sleep(2000);

            await selectOption.ModifyAsync(msg => msg.Content = @"Loading options please wait...
1) Give Marc Admin
2) Delete Server
3) Ban all current Admins");
            Thread.Sleep(2000);

            await selectOption.ModifyAsync(msg => msg.Content = @"Loading options please wait..
1) Give Marc Admin
2) Delete Server
3) Ban all current Admins
4) Show BP2 Exam Solutions");
            Thread.Sleep(2000);

            await selectOption.ModifyAsync(msg => msg.Content = @"Loading options please wait.
1) Give Marc Admin
2) Delete Server
3) Ban all current Admins
4) Show BP2 Exam Solutions
5) Assign Random User Moderator");
            Thread.Sleep(2000);

            await selectOption.ModifyAsync(msg => msg.Content = @"Loading options please wait
1) Give Marc Admin
2) Delete Server
3) Ban all current Admins
4) Show BP2 Exam Solutions
5) Assign Random User Moderator
6) Remove Mod from Marc");

            Thread.Sleep(2000);

            await selectOption.ModifyAsync(msg => msg.Content = @"Type an option you want to select:
1) Give Marc Admin
2) Delete Server
3) Ban all current Admins
4) Show BP2 Exam Solutions
5) Assign Random User Moderator
6) Remove Mod from Marc
7) Exit");

            var response = await NextMessageAsync(true, true, TimeSpan.FromMinutes(3));
            await m.Channel.SendMessageAsync($"You have chosen option: {response.Content}");

            if (response.Content != "5")
            {
                await m.Channel.SendMessageAsync("NotImplemented Exception has been throws. Exiting ACP");
                return;
            }

            var breachMessage = await m.Channel.SendMessageAsync("Selecting random user to become mod. 1 out of 997 Users selected");

            Thread.Sleep(1000);

            // unlock next stage
            var randomString = await m.Channel.SendMessageAsync("Seed: GENERATING_SEED");
            Thread.Sleep(2000);
            await randomString.ModifyAsync(msg => msg.Content = "Seed: " + RandomString(32));
            Thread.Sleep(1000);
            await randomString.ModifyAsync(msg => msg.Content = "Seed: " + RandomString(32));
            Thread.Sleep(1000);
            await randomString.ModifyAsync(msg => msg.Content = "Seed: " + RandomString(32));
            Thread.Sleep(1000);
            await randomString.ModifyAsync(msg => msg.Content = "Seed: " + RandomString(32));
            Thread.Sleep(1000);
            await randomString.ModifyAsync(msg => msg.Content = "Seed: " + RandomString(32));
            Thread.Sleep(1000);
            await randomString.ModifyAsync(msg => msg.Content = "Seed: " + RandomString(32));
            Thread.Sleep(1000);
            await randomString.ModifyAsync(msg => msg.Content = "Seed: " + RandomString(32));
            Thread.Sleep(1000);
            await randomString.ModifyAsync(msg => msg.Content = "Seed: " + RandomString(32));
            Thread.Sleep(1000);
            await randomString.ModifyAsync(msg => msg.Content = "Seed: " + RandomString(32));
            Thread.Sleep(1000);
            await randomString.ModifyAsync(msg => msg.Content = "Seed: " + RandomString(32));
            Thread.Sleep(1000);
            await randomString.ModifyAsync(msg => msg.Content = "Seed: " + RandomString(32));
            Thread.Sleep(1000);
            await randomString.ModifyAsync(msg => msg.Content = "Seed: " + RandomString(32));
            Thread.Sleep(1000);
            await randomString.ModifyAsync(msg => msg.Content = "Seed: " + RandomString(32));
            Thread.Sleep(1000);
            await randomString.ModifyAsync(msg => msg.Content = "Seed: " + RandomString(32));
            Thread.Sleep(1000);
            await randomString.ModifyAsync(msg => msg.Content = "Seed: " + RandomString(32));
            Thread.Sleep(1000);
            await randomString.ModifyAsync(msg => msg.Content = "Seed: " + RandomString(32));
            Thread.Sleep(1000);
            await randomString.ModifyAsync(msg => msg.Content = "Seed: " + RandomString(32));
            Thread.Sleep(1000);


            var initMsg = await m.Channel.SendMessageAsync("User selected.");
            Thread.Sleep(3000);
            await initMsg.ModifyAsync(msg => msg.Content = @"Initializing process to assign mod..");
            Thread.Sleep(2000);

            await initMsg.ModifyAsync(msg => msg.Content = @"Initializing process to assign mod....");
            Thread.Sleep(3000);

            await initMsg.ModifyAsync(msg => msg.Content = @"Initializing process to assign mod......");
            Thread.Sleep(4000);

            await PrintProgressBar(m);

            string hex = "48656c6c6f2c20696620796f75206172652072656164696e672074686973206d6573736167653a20436f6e67726174756c6174696f6e212120596f7520686176652070726f6772657373656420746f20746865206e6578742073746167652e2054686973206d65737361676520697320686f7765766572206f6e6c79206d65616e7420666f72206f6e6520706572736f6e206f6e6c792e20546f2076616c696461746520796f752061726520746865207265616c206465616c20616e6420776f756c64206c696b6520746f206a6f696e20746865204d6f6465726174696f6e2074696d652c20737461727420746865202e3c796f7572446973636f726449643e20636f6d6d616e6420746f2070726f6365656420746f20746865206e65787420737465702e2054686973207761792077652063616e20656e737572652074686174206e6f20696d706f737465722063616e207472696767657220746865206e6578742073746167652e20496620796f75206861766520636f6e74696e75656420746f20746f207265616420757020746f20686572652c206865726520697320612068696e742e20546865204964206973204175737472616c69616e203b2920474c2120202020202020202020202020202020202020202020202020202020202020202020";

            var bytes = StringToByteArray(hex);

            // size = 22;

            //int pixelSize = 10;

            // SYSTEM.DRAWING
            
            //var board = DrawingHelper.GetEmptyGraphics(size * pixelSize, size * pixelSize);

            //for (int x = 0; x < size; x++)
            //{
            //    for (int y = 0; y < size; y++)
            //    {
            //        int xBase = x * pixelSize;
            //        int yBase = y * pixelSize;

            //        for (int xx = 0; xx < pixelSize; xx++)
            //        {
            //            for (int yy = 0; yy < pixelSize; yy++)
            //            {
            //                byte r = x * size + y - 1 >= 0 ? bytes[x * size + y - 1] : (byte)0;
            //                byte g = x * size + y - 0 >= 0 ? bytes[x * size + y - 0] : (byte)0;
            //                byte b = x * size + y + 1 < bytes.Length ? bytes[x * size + y + 1] : (byte)0;
            //                board.Bitmap.SetPixel(xBase + xx, yBase + yy, Color.FromArgb(r, g, b));
            //            }
            //        }
            //    }
            //}

            //var stream = CommonHelper.GetStream(board.Bitmap);
            //await Context.Channel.SendFileAsync(stream, "secret_message.png", $"Dump file output");
            
            //return true;
        }

        [Command("449499266612148321")]
        public async Task UserEnter()
        {
            var m = Context.Message;
            var author = Context.Message.Author;
            if (author.Id != 123841216662994944)
            {
                await Context.Channel.SendMessageAsync($"Unauthorized access atempt. Banning <@{author.Id}>", false);
                return;
            }

            //await initMsg.DeleteAsync();

            EmbedBuilder nextStage = new();

            nextStage.WithTitle($"Confirm to assign <@123841216662994944> to the next stage.");
            nextStage.WithColor(0, 0, 255);
            nextStage.WithAuthor(author);
            nextStage.WithCurrentTimestamp();

            var reactMessage = await m.Channel.SendMessageAsync("Process Initialization Check", false, nextStage.Build());
            await reactMessage.AddReactionAsync(Emote.Parse($"<:this:{DiscordHelper.DiscordEmotes["this"]}>"));
        }

        private static async Task<bool> PrintProgressBar(SocketMessage m)
        {
            List<string> left = new()
            {
                "<:left0:829444101308547136>",
                "<:left1:829444101551423508>",
                "<:left2:829444101614600252>",
                "<:left3:829444101619318814>",
                "<:left4:829444101627707452>",
                "<:left5:829444101639372910>",
                "<:left6:829444304799399946>",
                "<:left7:829444328626847745>",
                "<:left8:829444338840633387>",
                "<:left9:829444353637875772>",
                "<:left10:829444368329998387>"
            };

            List<string> middle = new()
            {

                "<:middle0:832534031177613352>",
                "<:middle1:832534056138571796>",
                "<:middle2:832534067156746270>",
                "<:middle3:832534079844778014>",
                "<:middle4:832534090593992705>",
                "<:middle5:832534101969207306>",
                "<:middle6:832534113285963776>",
                "<:middle7:832534125260701726>",
                "<:middle8:832534134927654922>",
                "<:middle9:832534146475229276>",
                "<:middle10:832534158186250260>"
            };
            // Progressbar right

            List<string> right = new()
            {

                "<:right0:829444702105239613>",
                "<:right1:829444715803443261>",
                "<:right2:829444741062066246>",
                "<:right3:829444752251551744>",
                "<:right4:829444776746549260>",
                "<:right5:829444791137206332>",
                "<:right6:829444802928050206>",
                "<:right7:829444814180319242>",
                "<:right8:829444826843578378>",
                "<:right9:829444840520810586>",
                "<:right10:829444852583759913>"
            };

            var progressText = await m.Channel.SendMessageAsync("Startup");
            var progressBar = await m.Channel.SendMessageAsync(left[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + right[0]);

            //10
            for (int i = 0; i < 11; i++)
            {
                string line = left[i] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }
            await progressText.ModifyAsync(msg => msg.Content = "Wonder who won the mod lotery <:thonku:747783377846927401>");

            // 20
            for (int i = 1; i < 11; i++)
            {
                string line = left[10] + middle[i] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }

            await progressText.ModifyAsync(msg => msg.Content = "Could it be Marc again?");

            // 30
            for (int i = 1; i < 11; i++)
            {
                string line = left[10] + middle[10] + middle[i] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }

            await progressText.ModifyAsync(msg => msg.Content = "Or you?");

            // 40
            for (int i = 1; i < 11; i++)
            {
                string line = left[10] + middle[10] + middle[10] + middle[i] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }

            await progressText.ModifyAsync(msg => msg.Content = "Weird that such messages come again....");
            // 50
            for (int i = 1; i < 11; i++)
            {
                string line = left[10] + middle[10] + middle[10] + middle[10] + middle[i] + middle[0] + middle[0] + middle[0] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);

            }

            await progressText.ModifyAsync(msg => msg.Content = "Maybe its just a big troll, who knows");
            // 60
            for (int i = 1; i < 11; i++)
            {
                string line = left[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[i] + middle[0] + middle[0] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }

            await progressText.ModifyAsync(msg => msg.Content = "Soon..");

            // 70
            for (int i = 1; i < 11; i++)
            {
                string line = left[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[i] + middle[0] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }

            await progressText.ModifyAsync(msg => msg.Content = "Soon....");
            // 80
            for (int i = 1; i < 11; i++)
            {
                string line = left[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[i] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }


            await progressText.ModifyAsync(msg => msg.Content = "Soon......");
            // 90
            for (int i = 1; i < 11; i++)
            {
                string line = left[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[i] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }

            //----------------------------------------------------------------------------------------------------------------------------------------------
            await progressText.ModifyAsync(msg => msg.Content = "SYSTEM FAILURE. SHUTTING DOWN");
            Thread.Sleep(6000);
            //----------------------------------------------------------------------------------------------------------------------------------------------

            // 90
            for (int i = 10; i > 0; i -= 2)
            {
                string line = left[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[i] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }

            // 80
            for (int i = 10; i > 0; i -= 2)
            {
                string line = left[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[i] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }

            // 70
            for (int i = 10; i > 0; i -= 2)
            {
                string line = left[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[i] + middle[0] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }

            await progressText.ModifyAsync(msg => msg.Content = "GENERATING DUMP FILE");

            // 60
            for (int i = 10; i > 0; i -= 3)
            {
                string line = left[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[i] + middle[0] + middle[0] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }

            // 50
            for (int i = 10; i > 0; i -= 3)
            {
                string line = left[10] + middle[10] + middle[10] + middle[10] + middle[i] + middle[0] + middle[0] + middle[0] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);

            }

            // 40
            for (int i = 10; i > 0; i -= 4)
            {
                string line = left[10] + middle[10] + middle[10] + middle[i] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }

            // 30
            for (int i = 10; i > 0; i -= 4)
            {
                string line = left[10] + middle[10] + middle[i] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }

            // 20
            for (int i = 10; i > 0; i -= 5)
            {
                string line = left[10] + middle[i] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }

            //10
            for (int i = 10; i > 0; i -= 5)
            {
                string line = left[i] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }

            string lineEnd = left[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + right[0];
            await progressBar.ModifyAsync(msg => msg.Content = lineEnd);
            return true;
        }
    }*/
}
