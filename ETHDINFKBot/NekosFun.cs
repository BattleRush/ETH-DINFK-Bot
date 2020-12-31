using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace ETHDINFKBot
{
    public class NekosFun
    {
        public string GetLink(string tag)
        {
            using (WebClient client = new WebClient())
            {
                var json = client.DownloadString(new Uri($"http://api.nekos.fun:8080/api/{tag}"));
                var jsonInfo = JsonConvert.DeserializeObject<Rootobject>(json);
                //Console.WriteLine("Got link: " + jsonInfo.image);

                return jsonInfo.image;
            }
        }
    }


    public class Rootobject
    {
        public string image { get; set; }
    }

}
