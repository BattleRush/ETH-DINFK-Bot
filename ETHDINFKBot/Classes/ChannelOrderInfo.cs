using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Classes
{
    public class ChannelOrderInfo
    {
        public ulong ChannelId { get; set; }
        public string ChannelName { get; set; }
        public ulong CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int Position { get; set; }
    }
}
