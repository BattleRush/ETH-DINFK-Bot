// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
using System;
using System.Collections.Generic;

namespace ETHDINFKBot.Helpers.Food
{
    public class Category
    {
        public string __typename { get; set; }
        public string id { get; set; }
        public List<Item> items { get; set; }
    }

    public class ClimatePrediction
    {
        public string __typename { get; set; }
        public string rating { get; set; }
    }

    public class DailyRecipy
    {
        public string __typename { get; set; }
        public DateTime date { get; set; }
        public bool isOpen { get; set; }
        public Recipe recipe { get; set; }
    }

    public class DigitalMenu
    {
        public string __typename { get; set; }
        public string id { get; set; }
        public string displayHealthIcon { get; set; }
        public bool showClimateRatingColors { get; set; }
        public object weeklyNotes { get; set; }
        public string title { get; set; }
        public string label { get; set; }
        public List<Category> categories { get; set; }
    }

    public class Item
    {
        public string __typename { get; set; }
        public string id { get; set; }
        public string displayName { get; set; }
        public List<DailyRecipy> dailyRecipies { get; set; }
    }

    public class Kitchen
    {
        public string __typename { get; set; }
        public string id { get; set; }
        public string publicLabel { get; set; }
        public string logoUrl { get; set; }
        public DigitalMenu digitalMenu { get; set; }
        public string slug { get; set; }
        public Location location { get; set; }
    }

    public class Location
    {
        public string __typename { get; set; }
        public string id { get; set; }
        public Kitchen kitchen { get; set; }
        public string slug { get; set; }
    }

    public class PageProps
    {
        public Query query { get; set; }
        public Range range { get; set; }
    }

    public class Query
    {
        public Location location { get; set; }
        public string __typename { get; set; }
    }

    public class Range
    {
        public DateTime from { get; set; }
        public DateTime to { get; set; }
    }

    public class Recipe
    {
        public string __typename { get; set; }
        public string id { get; set; }
        public bool isHealthy { get; set; }
        public string menuLineDescription { get; set; }
        public string title_de { get; set; }
        public ClimatePrediction climatePrediction { get; set; }
        public string slug { get; set; }
        public Kitchen kitchen { get; set; }
        public string allergens { get; set; } // TODO Check if really a list
        public bool isVegan { get; set; }
        public bool isVegetarian { get; set; }
    }

    public class Food2050WeeklyResponse
    {
        public PageProps pageProps { get; set; }
        public bool __N_SSP { get; set; }
    }
}