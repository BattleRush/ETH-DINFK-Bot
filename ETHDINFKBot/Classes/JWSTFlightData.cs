using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Classes
{
    public class JWSTFlightData
    {
        public JWSTFlightDataInfo[] info { get; set; }
    }

    public class JWSTFlightDataInfo
    {
        public string NOTE { get; set; }
        public int elapsedSeconds { get; set; }
        public float elapsedMinutes { get; set; }
        public float elapsedHours { get; set; }
        public float elapsedDays { get; set; }
        public int distanceEarthCenterKm { get; set; }
        public int altitudeKm { get; set; }
        public float velocityKmSec { get; set; }
        public int distanceTravelledKm { get; set; }
        public string timeStampUtc { get; set; }
    }

}
