// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
using System.Collections.Generic;
namespace ETHDINFKBot.Helpers.Food
{
    public class MenuCategory
    {
        public string __typename { get; set; }
        public string title { get; set; }
    }

    public class MenuClimatePrediction
    {
        public string __typename { get; set; }
        public string rating { get; set; }
        public double temperatureChange { get; set; }
    }

    public class MenuPageProps
    {
        public MenuRecipe recipe { get; set; }
    }

    public class MenuPrice
    {
        public string __typename { get; set; }
        public string id { get; set; }
        public double amount { get; set; }
        public string currencyCode { get; set; }
        public MenuCategory category { get; set; }
    }

    public class MenuRecipe
    {
        public string __typename { get; set; }
        public string id { get; set; }
        public MenuClimatePrediction climatePrediction { get; set; }
        public object producerStoryUrl { get; set; }
        public bool? isVegan { get; set; }
        public bool? isVegetarian { get; set; }
        public string imageUrl { get; set; }
        public string title { get; set; }
        public string title_de { get; set; }
        public bool isImageVisible { get; set; }
        public string menuLineDescription { get; set; }
        public List<MenuPrice> prices { get; set; }
        public List<string> allergensList { get; set; }
        public double? energy { get; set; }
        public double? fat { get; set; }
        public double? carbohydrates { get; set; }
        public double? sugar { get; set; }
        public double? protein { get; set; }
        public double? salt { get; set; }
        public double? weight { get; set; }
    }

    public class Food2050MenuResponse
    {
        public MenuPageProps pageProps { get; set; }
        public bool __N_SSP { get; set; }
    }
}