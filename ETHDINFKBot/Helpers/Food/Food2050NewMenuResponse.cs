// --- Models for the initial JSON payload found in the HTML ---
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class NextData
{
    [JsonProperty("props")]
    public Props Props { get; set; }

    [JsonProperty("buildId")]
    public string BuildId { get; set; }
}

public class Props
{
    [JsonProperty("pageProps")]
    public PageProps PageProps { get; set; }
}

public class PageProps
{
    [JsonProperty("organisation")]
    public Organisation Organisation { get; set; }
}

public class Organisation
{
    [JsonProperty("outlet")]
    public Outlet Outlet { get; set; }
}

public class Outlet
{
    [JsonProperty("menuCategory")]
    public MenuCategory MenuCategory { get; set; }
}

public class MenuCategory
{
    [JsonProperty("calendar")]
    public Calendar Calendar { get; set; }
}

public class Calendar
{
    [JsonProperty("week")]
    public Week Week { get; set; }
}

public class Week
{
    [JsonProperty("daily")]
    public List<Daily> Daily { get; set; }
}

public class Daily
{
    [JsonProperty("from")]
    public FromDate From { get; set; }

    [JsonProperty("menuItems")]
    public List<InitialMenuItem> MenuItems { get; set; }
}

public class FromDate
{
    [JsonProperty("dateLocal")]
    public DateTime DateLocal { get; set; }
}

public class InitialMenuItem
{
    [JsonProperty("category")]
    public InitialMenuItemCategory Category { get; set; }
    
    [JsonProperty("detailUrl")]
    public string DetailUrl { get; set; }

    [JsonProperty("dish")]
    public InitialDish Dish { get; set; }
}

public class InitialMenuItemCategory
{
    [JsonProperty("name")]
    public string Name { get; set; }
}

public class InitialDish
{
    [JsonProperty("name")]
    public string Name { get; set; }
    
    [JsonProperty("allergens")]
    public List<AllergenWrapper> Allergens { get; set; }
}


// --- Models for the detailed menu JSON response ---

public class Food2050MenuDetailResponse
{
    [JsonProperty("pageProps")]
    public MenuDetailPageProps PageProps { get; set; }
}

public class MenuDetailPageProps
{
    [JsonProperty("organisation")]
    public MenuDetailOrganisation Organisation { get; set; }
}

public class MenuDetailOrganisation
{
    [JsonProperty("outlet")]
    public MenuDetailOutlet Outlet { get; set; }
}

public class MenuDetailOutlet
{
    [JsonProperty("menuCategory")]
    public MenuDetailMenuCategory MenuCategory { get; set; }
}

public class MenuDetailMenuCategory
{
    [JsonProperty("menuItem")]
    public MenuItemDetail MenuItem { get; set; }
}

public class MenuItemDetail
{
    [JsonProperty("dish")]
    public DishDetail Dish { get; set; }

    [JsonProperty("prices")]
    public List<Price> Prices { get; set; }
}

public class DishDetail
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("isVegan")]
    public bool IsVegan { get; set; }

    [JsonProperty("isVegetarian")]
    public bool IsVegetarian { get; set; }

    [JsonProperty("imageUrl")]
    public string ImageUrl { get; set; }

    [JsonProperty("stats")]
    public Stats Stats { get; set; }
    
    [JsonProperty("allergens")]
    public List<AllergenWrapper> Allergens { get; set; }
}

public class Stats
{
    [JsonProperty("servingWeight")]
    public Measurement ServingWeight { get; set; }

    [JsonProperty("energy")]
    public Measurement Energy { get; set; }

    [JsonProperty("fat")]
    public Measurement Fat { get; set; }

    [JsonProperty("carbohydrates")]
    public Measurement Carbohydrates { get; set; }

    [JsonProperty("sugar")]
    public Measurement Sugar { get; set; }

    [JsonProperty("protein")]
    public Measurement Protein { get; set; }

    [JsonProperty("salt")]
    public Measurement Salt { get; set; }
}

public class Measurement
{
    [JsonProperty("amount")]
    public double? Amount { get; set; }
}

public class Price
{
    [JsonProperty("amount")]
    public string Amount { get; set; }

    [JsonProperty("priceCategory")]
    public PriceCategory PriceCategory { get; set; }
}

public class PriceCategory
{
    [JsonProperty("name")]
    public string Name { get; set; }
}

public class AllergenWrapper
{
    [JsonProperty("allergen")]
    public Allergen Allergen { get; set; }
}

public class Allergen
{
    [JsonProperty("externalId")]
    public string ExternalId { get; set; }
}