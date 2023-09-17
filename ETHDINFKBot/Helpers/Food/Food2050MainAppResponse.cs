// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
using System.Collections.Generic;

namespace ETHDINFKBot.Helpers.Food
{
    public class AppLocation
    {
        public string __typename { get; set; }
        public string id { get; set; }
        public string slug { get; set; }
        public string title { get; set; }
    }

    public class AppPageProps
    {
        public List<AppLocation> locations { get; set; }
        public string __typename { get; set; }
    }

    public class AppProps
    {
        public AppPageProps pageProps { get; set; }
        public bool __N_SSP { get; set; }
    }

    public class AppQuery
    {
    }

    public class Food2050MainAppResponse
    {
        public AppProps props { get; set; }
        public string page { get; set; }
        public AppQuery query { get; set; }
        public string buildId { get; set; }
        public bool isFallback { get; set; }
        public bool gssp { get; set; }
        public List<object> scriptLoader { get; set; }
    }
}