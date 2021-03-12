using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Modules
{
    [Group("place")]
    public class PlaceModule : ModuleBase<SocketCommandContext>
    {
        [Command("view")]
        public async Task ViewBoard()
        {

        }

        [Command("zoom")]
        public async Task ZoomIntoTheBoard(string x, string y, int size = 1000)
        {
            // size can be between 10 and 1000
            // accept hex or int as cord

        }

        [Command("place")]
        public async Task PlaceColor(string x, string y, int color)
        {

        }

    }
}
