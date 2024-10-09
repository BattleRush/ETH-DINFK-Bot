

using System.Collections.Generic;
using Newtonsoft.Json;

namespace ETHDINFKBot.Helpers.Food
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class AddonArray
    {
        public string name { get; set; }

        [JsonProperty("price-unit-code")]
        public int priceunitcode { get; set; }

        [JsonProperty("price-unit-desc")]
        public string priceunitdesc { get; set; }

        [JsonProperty("price-unit-desc-short")]
        public string priceunitdescshort { get; set; }

        [JsonProperty("price-array")]
        public List<PriceArray> pricearray { get; set; }
    }

    public class AllergenArray
    {
        public int code { get; set; }
        public int position { get; set; }

        [JsonProperty("desc-short")]
        public string descshort { get; set; }
        public string desc { get; set; }
    }

    public class DayOfWeekArray
    {
        [JsonProperty("day-of-week-code")]
        public int dayofweekcode { get; set; }

        [JsonProperty("day-of-week-desc")]
        public string dayofweekdesc { get; set; }

        [JsonProperty("day-of-week-desc-short")]
        public string dayofweekdescshort { get; set; }

        [JsonProperty("opening-hour-array")]
        public List<OpeningHourArray> openinghourarray { get; set; }
    }

    public class FishingMethodArray
    {
        public int code { get; set; }
        public int position { get; set; }

        [JsonProperty("desc-short")]
        public string descshort { get; set; }
        public string desc { get; set; }

        [JsonProperty("origin-array")]
        public List<OriginArray> originarray { get; set; }
    }

    public class LineArray
    {
        public string name { get; set; }
        public ETHMeal meal { get; set; }
    }

    public class ETHMeal
    {
        [JsonProperty("line-id")]
        public int lineid { get; set; }
        public string name { get; set; }
        public string description { get; set; }

        [JsonProperty("price-unit-code")]
        public int priceunitcode { get; set; }

        [JsonProperty("price-unit-desc")]
        public string priceunitdesc { get; set; }

        [JsonProperty("price-unit-desc-short")]
        public string priceunitdescshort { get; set; }

        [JsonProperty("meal-price-array")]
        public List<MealPriceArray> mealpricearray { get; set; }

        [JsonProperty("meal-class-array")]
        public List<MealClassArray> mealclassarray { get; set; }

        [JsonProperty("allergen-array")]
        public List<AllergenArray> allergenarray { get; set; }

        [JsonProperty("meat-type-array")]
        public List<MeatTypeArray> meattypearray { get; set; }

        [JsonProperty("image-url")]
        public string imageurl { get; set; }
        public double? energy { get; set; }
        public double? proteins { get; set; }
        public double? fat { get; set; }

        [JsonProperty("saturated-fatty-acids")]
        public double? saturatedfattyacids { get; set; }

        public double? carbohydrates { get; set; }

        public double? sugar { get; set; }
        public double? salt { get; set; }

        [JsonProperty("fishing-method-array")]
        public List<FishingMethodArray> fishingmethodarray { get; set; }

        [JsonProperty("addon-array")]
        public List<AddonArray> addonarray { get; set; }
    }

    public class MealClassArray
    {
        public int code { get; set; }
        public int position { get; set; }

        [JsonProperty("desc-short")]
        public string descshort { get; set; }
        public string desc { get; set; }
    }

    public class MealPriceArray
    {
        public double price { get; set; }

        [JsonProperty("customer-group-code")]
        public int customergroupcode { get; set; }

        [JsonProperty("customer-group-position")]
        public int customergroupposition { get; set; }

        [JsonProperty("customer-group-desc")]
        public string customergroupdesc { get; set; }

        [JsonProperty("customer-group-desc-short")]
        public string customergroupdescshort { get; set; }
    }

    public class MealTimeArray
    {
        public string name { get; set; }

        [JsonProperty("time-from")]
        public string timefrom { get; set; }

        [JsonProperty("time-to")]
        public string timeto { get; set; }

        [JsonProperty("line-array")]
        public List<LineArray> linearray { get; set; }
        public ETHMenu menu { get; set; }
    }

    public class MeatTypeArray
    {
        public int code { get; set; }
        public int position { get; set; }

        [JsonProperty("desc-short")]
        public string descshort { get; set; }
        public string desc { get; set; }

        [JsonProperty("origin-array")]
        public List<OriginArray> originarray { get; set; }
    }

    public class ETHMenu
    {
        [JsonProperty("menu-url")]
        public string menuurl { get; set; }
    }

    public class OpeningHourArray
    {
        [JsonProperty("time-from")]
        public string timefrom { get; set; }

        [JsonProperty("time-to")]
        public string timeto { get; set; }

        [JsonProperty("meal-time-array")]
        public List<MealTimeArray> mealtimearray { get; set; }
    }

    public class OriginArray
    {
        public int code { get; set; }
        public int position { get; set; }

        [JsonProperty("desc-short")]
        public string descshort { get; set; }
        public string desc { get; set; }
    }

    public class PriceArray
    {
        public double price { get; set; }

        [JsonProperty("customer-group-code")]
        public int customergroupcode { get; set; }

        [JsonProperty("customer-group-position")]
        public int customergroupposition { get; set; }

        [JsonProperty("customer-group-desc")]
        public string customergroupdesc { get; set; }

        [JsonProperty("customer-group-desc-short")]
        public string customergroupdescshort { get; set; }
    }

    public class ETHFoodResponse
    {
        [JsonProperty("weekly-rota-array")]
        public List<WeeklyRotaArray> weeklyrotaarray { get; set; }
    }

    public class WeeklyRotaArray
    {
        [JsonProperty("weekly-rota-id")]
        public int weeklyrotaid { get; set; }

        [JsonProperty("facility-id")]
        public int facilityid { get; set; }

        [JsonProperty("valid-from")]
        public string validfrom { get; set; }

        [JsonProperty("valid-to")]
        public string validto { get; set; }

        [JsonProperty("day-of-week-array")]
        public List<DayOfWeekArray> dayofweekarray { get; set; }
    }
}