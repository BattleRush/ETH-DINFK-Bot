using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Classes
{
    public class JWSTDeploymentInfos
    {
        public Info[] info { get; set; }
    }

    public class Info
    {
        public string eventId { get; set; }
        public string title { get; set; }
        public string subtitle { get; set; }
        public string nominalEventTime { get; set; }
        public string details { get; set; }
        public string thumbnailUrl { get; set; }
        public string stateImageUrlPrevious { get; set; }
        public string stateImageUrl { get; set; }
        public string moreDetails { get; set; }
        public Relatedimage[] relatedImages { get; set; }
        public Relatedvideo[] relatedVideos { get; set; }
        public Relatedlink[] relatedLinks { get; set; }
        public string approxStartTimeRelToLaunch { get; set; }
        public string approxEndTimeRelToLaunch { get; set; }
        public int approxDistanceFromEarthMiles { get; set; }
        public int approxDuration { get; set; }
        public int approxSpeedMph { get; set; }
    }

    public class Relatedimage
    {
        public string name { get; set; }
        public string url { get; set; }
        public string description { get; set; }
    }

    public class Relatedvideo
    {
        public string name { get; set; }
        public string url { get; set; }
        public string description { get; set; }
    }

    public class Relatedlink
    {
        public string name { get; set; }
        public string url { get; set; }
        public string description { get; set; }
        public string target { get; set; }
    }

}
