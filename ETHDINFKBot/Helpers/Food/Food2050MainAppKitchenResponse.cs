// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
using System.Collections.Generic;

namespace ETHDINFKBot.Helpers.Food
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class KitchenCategory
    {
        public string __typename { get; set; }
        public string id { get; set; }
        public string slug { get; set; }
        public string displayName { get; set; }
        public List<KitchenItem> items { get; set; }
    }

    public class KitchenDigitalMenu
    {
        public string __typename { get; set; }
        public string id { get; set; }
        public string slug { get; set; }
        public string label { get; set; }
        public List<KitchenCategory> categories { get; set; }
    }

    public class KitchenItem
    {
        public string __typename { get; set; }
        public string id { get; set; }
        public string slug { get; set; }
        public string displayName { get; set; }
        public KitchenRecipe recipe { get; set; }
    }

    public class KitchenKitchen
    {
        public string __typename { get; set; }
        public string publicLabel { get; set; }
        public string id { get; set; }
        public List<KitchenDigitalMenu> digitalMenus { get; set; }
        public string slug { get; set; }
        public KitchenLocation location { get; set; }
    }

    public class KitchenLocation
    {
        public string __typename { get; set; }
        public string title { get; set; }
        public KitchenKitchen kitchen { get; set; }
        public string slug { get; set; }
    }

    public class KitchenPageProps
    {
        public KitchenLocation location { get; set; }
    }

    public class KitchenProps
    {
        public KitchenPageProps pageProps { get; set; }
        public bool __N_SSP { get; set; }
    }

    public class KitchenQuery
    {
        public string locationSlug { get; set; }
        public string kitchenSlug { get; set; }
    }

    public class KitchenRecipe
    {
        public string __typename { get; set; }
        public string id { get; set; }
    }

    public class Food2050MainAppKitchenResponse
    {
        public KitchenProps props { get; set; }
        public string page { get; set; }
        public KitchenQuery query { get; set; }
        public string buildId { get; set; }
        public bool isFallback { get; set; }
        public bool gssp { get; set; }
        public List<object> scriptLoader { get; set; }
    }
}