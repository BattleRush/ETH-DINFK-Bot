using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Classes
{

    public class JWSTStatus
    {
        public Currentstate currentState { get; set; }
    }

    public class Currentstate
    {
        public string launchDateTimeString { get; set; }
        public int currentDeployTableIndex { get; set; }
        public string tempWarmSide1C { get; set; }
        public string tempWarmSide2C { get; set; }
        public string tempCoolSide1C { get; set; }
        public string tempCoolSide2C { get; set; }
        public bool tempsShow { get; set; }
        public string last { get; set; }
    }

}
