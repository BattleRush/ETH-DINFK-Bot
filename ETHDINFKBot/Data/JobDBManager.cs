using ETHBot.DataLayer;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Data
{
    public class JobDBManager
    {
        private static JobDBManager _instance;
        private static object syncLock = new object();
        private readonly ILogger _logger = new Logger<JobDBManager>(Program.Logger);

        public static JobDBManager Instance()
        {
            lock (syncLock)
            {
                if (_instance == null)
                {
                    _instance = new JobDBManager();
                }
            }

            return _instance;
        }



    }
}
