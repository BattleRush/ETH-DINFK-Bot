// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
using System.Collections.Generic;

namespace ETHDINFKBot.Helpers.Food
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class LocationLocation
    {
        public string __typename { get; set; }
        public List<LocationOutlet> outlets { get; set; }
        public string slug { get; set; }
    }

    public class LocationOutlet
    {
        public string __typename { get; set; }
        public string id { get; set; }
        public string publicLabel { get; set; }
        public string name { get; set; }
        public string slug { get; set; }
        public LocationLocation location { get; set; }
    }

    public class LocationPageProps
    {
        public LocationLocation location { get; set; }
    }

    public class LocationProps
    {
        public LocationPageProps pageProps { get; set; }
        public bool __N_SSP { get; set; }
    }

    public class LocationQuery
    {
        public string locationSlug { get; set; }
    }

    public class Food2050MainAppLocationResponse
    {
        public LocationProps props { get; set; }
        public string page { get; set; }
        public LocationQuery query { get; set; }
        public string buildId { get; set; }
        public bool isFallback { get; set; }
        public bool gssp { get; set; }
        public List<object> scriptLoader { get; set; }
    }


}