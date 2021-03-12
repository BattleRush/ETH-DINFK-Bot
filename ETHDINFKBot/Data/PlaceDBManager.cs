using ETHBot.DataLayer;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Data
{
    public class PlaceDBManager
    {
        private static PlaceDBManager _instance;
        private static object syncLock = new object();
        private readonly ILogger _logger = new Logger<PlaceDBManager>(Program.Logger);

        public static PlaceDBManager Instance()
        {
            lock (syncLock)
            {
                if (_instance == null)
                {
                    _instance = new PlaceDBManager();
                }
            }

            return _instance;
        }



        public void PlacePixel(int x, int y, int color)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    //return context.DiscordUsers.SingleOrDefault(i => i.DiscordUserId == id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                //return null;
            }
        }

    }
}
